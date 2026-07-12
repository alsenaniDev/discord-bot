import { DiscordSDK } from '@discord/embedded-app-sdk';
import { exchangeActivitiesCode, exchangeActivityCode, exchangeLocalActivityProfile, isActivitiesApiConfigured, setActivitiesAccessToken, setActivityRequestContext } from './api';
import type { ActivityIdentity } from '../types';

const clientId = (import.meta.env.VITE_DISCORD_CLIENT_ID as string | undefined)?.trim();
const activitiesApiBase = (import.meta.env.VITE_ACTIVITIES_API_BASE_URL as string | undefined)?.trim();

const query = () => new URLSearchParams(window.location.search);
const hasDiscordFrameId = () => query().has('frame_id');
const isDevEnvironment = () => import.meta.env.DEV || ((import.meta.env.VITE_ENVIRONMENT as string | undefined)?.trim().toLowerCase() === 'development');

export const getRequestedLocalProfile = () => query().get('localProfile')?.trim() || '';
export const isLocalBrowserModeAvailable = () => isDevEnvironment() && !hasDiscordFrameId() && isActivitiesApiConfigured();

export async function initializeDiscordActivity(localProfileName?: string): Promise<ActivityIdentity> {
  if (isLocalBrowserModeAvailable()) {
    const profileName = (localProfileName?.trim() || getRequestedLocalProfile()).trim();
    if (!profileName) throw new Error('اختر ملف اختبار محلي للمتابعة.');
    const token = await exchangeLocalActivityProfile(profileName);
    setActivitiesAccessToken(token.accessToken);
    setActivityRequestContext({
      guildId: token.guildDiscordId,
      channelId: token.channelDiscordId,
      activityInstanceId: token.activityInstanceId
    });
    return {
      accessToken: token.accessToken,
      activitiesAccessToken: token.accessToken,
      activitiesTokenExpiresAt: token.expiresAt,
      activityInstanceId: token.activityInstanceId,
      userId: token.user.discordUserId,
      username: token.user.username,
      avatarUrl: token.user.avatarUrl,
      guildId: token.guildDiscordId,
      channelId: token.channelDiscordId,
      isLocalBrowserMode: true,
      localProfileName: profileName
    };
  }

  if (!clientId) throw new Error('لم يتم إعداد معرّف تطبيق ديسكورد للواجهة.');
  const discordSdk = new DiscordSDK(clientId);
  await discordSdk.ready();
  if (!discordSdk.guildId || !discordSdk.channelId) throw new Error('يجب فتح مركز الألعاب من داخل روم في سيرفر ديسكورد.');
  const { code } = await discordSdk.commands.authorize({ client_id: clientId!, response_type: 'code', state: crypto.randomUUID(), prompt: 'none', scope: ['identify'] });
  const activityInstanceId = discordSdk.instanceId?.trim() || null;
  setActivityRequestContext({ guildId: discordSdk.guildId, channelId: discordSdk.channelId, activityInstanceId });
  const token = activitiesApiBase
    ? await exchangeActivitiesCode(code, discordSdk.guildId, discordSdk.channelId, activityInstanceId)
    : null;
  if (activitiesApiBase && !token?.discordAccessToken) throw new Error('تعذر استلام رمز Discord من خدمة Activities.');
  const discordAccessToken = token?.discordAccessToken ?? (await exchangeActivityCode(code)).accessToken;
  if (token?.accessToken) setActivitiesAccessToken(token.accessToken);
  const authentication = await discordSdk.commands.authenticate({ access_token: discordAccessToken });
  if (!authentication?.user?.id) throw new Error('تعذر تسجيل الدخول إلى ديسكورد.');
  const avatarHash = (authentication.user as { avatar?: string | null }).avatar;
  const avatarUrl = avatarHash ? `https://cdn.discordapp.com/avatars/${authentication.user.id}/${avatarHash}.png` : null;
  return {
    accessToken: discordAccessToken,
    activitiesAccessToken: token?.accessToken,
    activitiesTokenExpiresAt: token?.expiresAt,
    activityInstanceId,
    userId: authentication.user.id,
    username: authentication.user.global_name ?? authentication.user.username,
    avatarUrl,
    guildId: discordSdk.guildId,
    channelId: discordSdk.channelId,
    isLocalBrowserMode: false
  };
}
