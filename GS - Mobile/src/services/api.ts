import type { TrafficSummary } from '@/types/domain';

const baseUrl = process.env.EXPO_PUBLIC_API_BASE_URL?.replace(/\/$/, '');
const scenarioId = process.env.EXPO_PUBLIC_SCENARIO_ID ?? 'fluxia_stage6_c2';

function isTrafficSummary(value: unknown): value is TrafficSummary {
  if (typeof value !== 'object' || value === null) return false;
  if (!('scenarioId' in value) || !('environment' in value) || !('origin' in value) || !('asOfUtc' in value) || !('metrics' in value) || !('limitations' in value)) return false;
  return typeof value.scenarioId === 'string' && typeof value.environment === 'string' && typeof value.origin === 'string' && typeof value.asOfUtc === 'string' && Array.isArray(value.metrics) && Array.isArray(value.limitations);
}

export async function getTrafficSummary(): Promise<TrafficSummary | null> {
  if (!baseUrl) return null;
  const response = await fetch(`${baseUrl}/api/v1/traffic-summary?scenarioId=${encodeURIComponent(scenarioId)}`);
  if (response.status === 404) return null;
  if (!response.ok) throw new Error(`Falha ao consultar o resumo do trânsito (${response.status}).`);
  const body: unknown = await response.json();
  if (!isTrafficSummary(body)) throw new Error('A API retornou um resumo de trânsito em formato inesperado.');
  return body;
}
