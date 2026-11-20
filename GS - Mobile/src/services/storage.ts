import { File, Paths } from 'expo-file-system';
import { Platform } from 'react-native';

import { isIncidentCategory } from '@/utils/validation';
import type { Identity, IncidentReceipt } from '@/types/domain';

const WEB_STORAGE_KEY = '@citizentech/data-v1';
type LocalData = { identity: Identity | null; receipts: IncidentReceipt[] };

async function readStoredText(): Promise<string | null> {
  if (Platform.OS === 'web') return typeof localStorage === 'undefined' ? null : localStorage.getItem(WEB_STORAGE_KEY);
  const file = new File(Paths.document, 'citizentech-data.json');
  return file.exists ? file.text() : null;
}

async function writeStoredText(text: string): Promise<void> {
  if (Platform.OS === 'web') {
    if (typeof localStorage !== 'undefined') localStorage.setItem(WEB_STORAGE_KEY, text);
    return;
  }
  const file = new File(Paths.document, 'citizentech-data.json');
  if (!file.exists) file.create();
  await file.write(text);
}

async function readData(): Promise<LocalData> {
  const raw = await readStoredText();
  if (!raw) return { identity: null, receipts: [] };
  try {
    const parsed: unknown = JSON.parse(raw);
    if (typeof parsed !== 'object' || parsed === null) return { identity: null, receipts: [] };
    const data = parsed as { identity?: unknown; receipts?: unknown };
    return {
      identity: isIdentity(data.identity) ? data.identity : null,
      receipts: Array.isArray(data.receipts) ? data.receipts.filter(isReceipt) : [],
    };
  } catch {
    return { identity: null, receipts: [] };
  }
}

async function writeData(data: LocalData): Promise<void> {
  await writeStoredText(JSON.stringify(data));
}

function isIdentity(value: unknown): value is Identity {
  if (typeof value !== 'object' || value === null || !('displayName' in value)) return false;
  const name = value.displayName;
  return typeof name === 'string' && name.trim().length >= 2 && name.length <= 40;
}

function isReceipt(value: unknown): value is IncidentReceipt {
  if (typeof value !== 'object' || value === null) return false;
  if (!('id' in value) || !('category' in value) || !('description' in value) || !('createdAt' in value) || !('locationIncluded' in value) || !('status' in value)) return false;
  return typeof value.id === 'string' && isIncidentCategory(value.category) && typeof value.description === 'string' && typeof value.createdAt === 'string' && typeof value.locationIncluded === 'boolean' && value.status === 'Recebido (demonstração local)';
}

export async function getIdentity(): Promise<Identity | null> {
  return (await readData()).identity;
}

export async function saveIdentity(identity: Identity): Promise<void> {
  const data = await readData();
  await writeData({ ...data, identity });
}

export async function getReceipts(): Promise<IncidentReceipt[]> {
  return (await readData()).receipts;
}

export async function getReceipt(id: string): Promise<IncidentReceipt | null> {
  const receipts = await getReceipts();
  return receipts.find((receipt) => receipt.id === id) ?? null;
}

export async function saveReceipt(receipt: IncidentReceipt): Promise<void> {
  const data = await readData();
  await writeData({ ...data, receipts: [receipt, ...data.receipts] });
}
