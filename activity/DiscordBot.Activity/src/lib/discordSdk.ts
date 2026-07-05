import { DiscordSDK } from '@discord/embedded-app-sdk';
import { exchangeActivityCode } from './api';
import type { ActivityIdentity } from '../types';

const clientId = (import.meta.env.VITE_DISCORD_CLIENT_ID as string | undefined)?.trim();
if (!clientId) throw new Error('لم يتم إعداد معرّف تطبيق ديسكورد للواجهة.');

export const discordSdk = new DiscordSDK(clientId);

export async function initializeDiscordActivity(): Promise<ActivityIdentity> {
  await discordSdk.ready();
  if (!discordSdk.guildId || !discordSdk.channelId) throw new Error('يجب فتح مركز الألعاب من داخل روم في سيرفر ديسكورد.');
  const { code } = await discordSdk.commands.authorize({ client_id: clientId!, response_type: 'code', state: crypto.randomUUID(), prompt: 'none', scope: ['identify'] });
  const token = await exchangeActivityCode(code);
  const authentication = await discordSdk.commands.authenticate({ access_token: token.accessToken });
  if (!authentication?.user?.id) throw new Error('تعذر تسجيل الدخول إلى ديسكورد.');
  return { accessToken: token.accessToken, userId: authentication.user.id, username: authentication.user.global_name ?? authentication.user.username, guildId: discordSdk.guildId, channelId: discordSdk.channelId };
}
