export const INCIDENT_CATEGORIES = [
  'Congestionamento',
  'Semáforo com defeito',
  'Colisão',
  'Obstrução na via',
  'Outro',
] as const;

export type IncidentCategory = (typeof INCIDENT_CATEGORIES)[number];

export type Identity = { displayName: string };

export type LocationFix = {
  latitude: number;
  longitude: number;
};

export type IncidentDraft = {
  category: IncidentCategory | '';
  description: string;
  location?: LocationFix;
};

export type IncidentReceipt = {
  id: string;
  category: IncidentCategory;
  description: string;
  createdAt: string;
  locationIncluded: boolean;
  status: 'Recebido (demonstração local)';
};

export type TrafficMetric = { label: string; value: number; unit?: string };

export type TrafficSummary = {
  scenarioId: string;
  environment: string;
  origin: string;
  asOfUtc: string;
  metrics: TrafficMetric[];
  limitations: string[];
};

export type RouteParams = {
  '/': undefined;
  '/occurrences': undefined;
  '/new-report': undefined;
  '/occurrences/[id]': { id: string };
};
