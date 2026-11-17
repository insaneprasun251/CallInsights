export interface CallInsight {
  templateKey: string;
  templateVersion: string;
  responseText: string | null;
  responseModel: string | null;
  respondedAtUtc: string | null;
}
