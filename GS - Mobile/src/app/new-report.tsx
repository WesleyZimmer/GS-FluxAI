import { useState } from 'react';
import { ActivityIndicator, Pressable, Text, TextInput, View } from 'react-native';
import { router } from 'expo-router';

import { ActionButton, Card, colors, Notice, Screen, textStyles, Title } from '@/components/ui';
import { requestCurrentLocation } from '@/services/location';
import { saveReceipt } from '@/services/storage';
import { INCIDENT_CATEGORIES, type IncidentCategory, type LocationFix } from '@/types/domain';
import { validateIncident } from '@/utils/validation';

export default function NewReportScreen() {
  const [category, setCategory] = useState<IncidentCategory | ''>('');
  const [description, setDescription] = useState('');
  const [location, setLocation] = useState<LocationFix | undefined>();
  const [locationMessage, setLocationMessage] = useState('');
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);
  const [locating, setLocating] = useState(false);

  async function addLocation() {
    setLocating(true); setLocationMessage('');
    try {
      const result = await requestCurrentLocation();
      if (result.ok) { setLocation(result.location); setLocationMessage('GPS confirmado. As coordenadas não serão armazenadas.'); }
      else { setLocation(undefined); setLocationMessage(result.message); }
    } catch { setLocation(undefined); setLocationMessage('Não foi possível consultar a localização. Você ainda pode enviar sem GPS.'); }
    finally { setLocating(false); }
  }

  async function submit() {
    const validation = validateIncident({ category, description, location });
    if (!validation.valid) { setError(validation.message); return; }
    if (!category) return;
    setSaving(true); setError('');
    const receipt = {
      id: `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
      category,
      description: description.trim(),
      createdAt: new Date().toISOString(),
      locationIncluded: Boolean(location),
      status: 'Recebido (demonstração local)' as const,
    };
    try {
      await saveReceipt(receipt);
      router.replace({ pathname: '/occurrences/[id]', params: { id: receipt.id } });
    } catch {
      setError('Não foi possível salvar. Confira o espaço do aparelho e tente novamente.');
      setSaving(false);
    }
  }

  return <Screen>
    <Title eyebrow="NOVA OCORRÊNCIA" subtitle="Conte o que você observou para apoiar a mobilidade urbana.">O que aconteceu?</Title>
    <Card>
      <Text style={textStyles.label}>Tipo de ocorrência</Text>
      <View style={{ gap: 8 }}>
        {INCIDENT_CATEGORIES.map((item) => {
          const selected = category === item;
          return <Pressable key={item} accessibilityRole="radio" accessibilityState={{ checked: selected }} onPress={() => setCategory(item)} style={{ borderRadius: 13, borderWidth: 1, borderColor: selected ? colors.primary : colors.border, backgroundColor: selected ? '#E0F2EA' : '#FFFFFF', padding: 13 }}><Text style={{ color: selected ? colors.primary : colors.ink, fontWeight: selected ? '800' : '600', fontSize: 15 }}>{selected ? '●  ' : '○  '}{item}</Text></Pressable>;
        })}
      </View>
      <Text style={[textStyles.label, { marginTop: 8 }]}>Descrição</Text>
      <TextInput accessibilityLabel="Descrição da ocorrência" multiline maxLength={500} onChangeText={setDescription} placeholder="Ex.: congestionamento na faixa da direita, próximo ao cruzamento…" placeholderTextColor={colors.muted} textAlignVertical="top" style={{ minHeight: 120, borderWidth: 1, borderColor: colors.border, borderRadius: 13, padding: 13, color: colors.ink, fontSize: 15, lineHeight: 22 }} value={description} />
      <Text style={[textStyles.small, { textAlign: 'right' }]}>{description.length}/500 · mínimo 10 caracteres</Text>
    </Card>
    <Card>
      <Text style={{ color: colors.ink, fontWeight: '800', fontSize: 17 }}>Adicionar localização</Text>
      <Text style={textStyles.muted}>Opcional. O app pedirá permissão somente se você tocar no botão. A coordenada exata fica apenas na memória durante o preenchimento.</Text>
      {locating ? <ActivityIndicator color={colors.primary} /> : <ActionButton label={location ? 'GPS confirmado ✓' : 'Usar minha localização'} secondary onPress={() => void addLocation()} />}
      {locationMessage ? <Text style={textStyles.small}>{locationMessage}</Text> : null}
    </Card>
    <Notice>Modo demonstração: o registro é salvo localmente; a API de envio ainda não está disponível nesta etapa.</Notice>
    {error ? <Notice danger>{error}</Notice> : null}
    <ActionButton label={saving ? 'Salvando…' : 'Enviar ocorrência'} disabled={saving || locating} onPress={() => void submit()} />
  </Screen>;
}
