/** Static manual billing details for Closed Beta — replace when admin billing settings exist. */
export interface ManualBillingConfig {
  bankName: string;
  accountName: string;
  iban: string;
  swift?: string;
  supportUrl: string;
  statusUrl: string;
  reviewSlaDays: string;
}

export const MANUAL_BILLING_CONFIG: ManualBillingConfig = {
  bankName: 'Example Bank',
  accountName: 'Discord Bot Platform LLC',
  iban: 'SA00 0000 0000 0000 0000 0000',
  swift: 'EXMPSAXX',
  supportUrl: 'https://discord.com',
  statusUrl: 'https://discordstatus.com',
  reviewSlaDays: '1–2 business days'
};

export function buildPaymentReferenceHint(guildId: string): string {
  return `DBP-${guildId.slice(0, 8).toUpperCase()}`;
}
