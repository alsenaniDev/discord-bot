import { DiscordSDK } from '@discord/embedded-app-sdk';
import { exchangeActivitiesCode, exchangeActivityCode, setActivitiesAccessToken } from './api';
import type { ActivityIdentity } from '../types';

const clientId = (import.meta.env.VITE_DISCORD_CLIENT_ID as string | undefined)?.trim();
const activitiesApiBase = (import.meta.env.VITE_ACTIVITIES_API_BASE_URL as string | undefined)?.trim();
if (!clientId) throw new Error('لم يتم إعداد معرّف تطبيق ديسكورد للواجهة.');

export const discordSdk = new DiscordSDK(clientId);

export async function initializeDiscordActivity(): Promise<ActivityIdentity> {
  await discordSdk.ready();
  if (!discordSdk.guildId || !discordSdk.channelId) throw new Error('يجب فتح مركز الألعاب من داخل روم في سيرفر ديسكورد.');
  const { code } = await discordSdk.commands.authorize({ client_id: clientId!, response_type: 'code', state: crypto.randomUUID(), prompt: 'none', scope: ['identify'] });
  const activityInstanceId = discordSdk.instanceId || null;
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
    channelId: discordSdk.channelId
  };
}
