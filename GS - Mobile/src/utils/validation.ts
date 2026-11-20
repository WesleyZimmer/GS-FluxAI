import { INCIDENT_CATEGORIES, type IncidentDraft, type IncidentCategory } from '@/types/domain';

export type ValidationResult = { valid: true } | { valid: false; message: string };

export function validateDisplayName(value: string): ValidationResult {
  const name = value.trim();
  if (name.length < 2) return { valid: false, message: 'Digite um nome com pelo menos 2 caracteres.' };
  if (name.length > 40) return { valid: false, message: 'O nome deve ter no máximo 40 caracteres.' };
  return { valid: true };
}

export function isIncidentCategory(value: unknown): value is IncidentCategory {
  return typeof value === 'string' && INCIDENT_CATEGORIES.some((category) => category === value);
}

export function validateIncident(draft: IncidentDraft): ValidationResult {
  if (!isIncidentCategory(draft.category)) return { valid: false, message: 'Selecione o tipo da ocorrência.' };
  const description = draft.description.trim();
  if (description.length < 10) return { valid: false, message: 'Descreva a ocorrência com pelo menos 10 caracteres.' };
  if (description.length > 500) return { valid: false, message: 'A descrição deve ter no máximo 500 caracteres.' };
  if (draft.location && (!Number.isFinite(draft.location.latitude) || !Number.isFinite(draft.location.longitude) || Math.abs(draft.location.latitude) > 90 || Math.abs(draft.location.longitude) > 180)) {
    return { valid: false, message: 'A localização recebida pelo aparelho é inválida.' };
  }
  return { valid: true };
}
