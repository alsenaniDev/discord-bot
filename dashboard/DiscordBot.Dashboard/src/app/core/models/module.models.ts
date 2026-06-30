export interface GuildModule {
  key: string;
  name: string;
  description: string;
  isEnabled: boolean;
  allowedByPlan: boolean;
  effectiveEnabled: boolean;
}

export interface UpdateGuildModuleRequest {
  isEnabled: boolean;
}
