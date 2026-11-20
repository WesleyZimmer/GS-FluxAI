import { useCallback, useState } from 'react';
import { ActivityIndicator, Pressable, Text, View } from 'react-native';
import { router, useFocusEffect } from 'expo-router';

import { ActionButton, Card, colors, Notice, Screen, textStyles, Title } from '@/components/ui';
import { getReceipts } from '@/services/storage';
import type { IncidentReceipt } from '@/types/domain';

export default function OccurrencesScreen() {
  const [items, setItems] = useState<IncidentReceipt[]>([]);
  const [state, setState] = useState<'loading' | 'ready' | 'error'>('loading');
  const load = useCallback(async () => {
    setState('loading');
    try { setItems(await getReceipts()); setState('ready'); }
    catch { setState('error'); }
  }, []);
  useFocusEffect(useCallback(() => { void load(); }, [load]));

  return <Screen>
    <Title eyebrow="REGISTROS" subtitle="Ocorrências criadas neste aparelho.">Minhas ocorrências</Title>
    <ActionButton label="＋  Nova ocorrência" onPress={() => router.push('/new-report')} />
    {state === 'loading' ? <Card><ActivityIndicator color={colors.primary} /><Text style={textStyles.muted}>Carregando ocorrências…</Text></Card> : null}
    {state === 'error' ? <Card><Notice danger>Não foi possível ler seus registros salvos.</Notice><ActionButton label="Tentar novamente" secondary onPress={() => void load()} /></Card> : null}
    {state === 'ready' && items.length === 0 ? <Card style={{ alignItems: 'center', paddingVertical: 30 }}><Text style={{ fontSize: 38 }}>🗒️</Text><Text style={{ color: colors.ink, fontSize: 18, fontWeight: '800' }}>Nenhum registro por aqui</Text><Text style={[textStyles.muted, { textAlign: 'center' }]}>Quando você criar uma ocorrência, ela aparecerá nesta lista.</Text><ActionButton label="Criar primeira ocorrência" onPress={() => router.push('/new-report')} /></Card> : null}
    {state === 'ready' ? items.map((item) => <Pressable key={item.id} accessibilityRole="button" onPress={() => router.push({ pathname: '/occurrences/[id]', params: { id: item.id } })}>
      <Card>
        <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' }}><Text style={{ color: colors.ink, fontSize: 17, fontWeight: '800', flex: 1 }}>{item.category}</Text><Text style={{ fontSize: 18 }}>›</Text></View>
        <Text numberOfLines={2} style={textStyles.muted}>{item.description}</Text>
        <Text style={textStyles.small}>{new Date(item.createdAt).toLocaleString('pt-BR')} · Demonstração local</Text>
      </Card>
    </Pressable>) : null}
  </Screen>;
}
