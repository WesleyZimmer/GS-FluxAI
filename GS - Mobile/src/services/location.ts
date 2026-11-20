import { requireOptionalNativeModule } from 'expo-modules-core';

import type { LocationFix } from '@/types/domain';

export type LocationResult =
  | { ok: true; location: LocationFix }
  | { ok: false; message: string };

type NativeLocationModule = {
  requestForegroundPermissionsAsync(): Promise<{ status: string }>;
  getCurrentPositionAsync(options: { accuracy: number }): Promise<{ coords: LocationFix }>;
};

export async function requestCurrentLocation(): Promise<LocationResult> {
  const locationModule = requireOptionalNativeModule<NativeLocationModule>('ExpoLocation');
  if (!locationModule) return { ok: false, message: 'Localização nativa indisponível neste ambiente. Você ainda pode enviar sem GPS.' };
  try {
    const permission = await locationModule.requestForegroundPermissionsAsync();
    if (permission.status !== 'granted') return { ok: false, message: 'A permissão de localização foi negada. Você ainda pode enviar sem GPS.' };
    const position = await locationModule.getCurrentPositionAsync({ accuracy: 3 });
    return { ok: true, location: position.coords };
  } catch {
    return { ok: false, message: 'Não foi possível obter a localização agora. Tente novamente ou envie sem GPS.' };
  }
}
