import { useEffect, useState } from 'react';
import { ActivityIndicator, Text } from 'react-native';
import { router, useLocalSearchParams } from 'expo-router';

import { ActionButton, Card, colors, Notice, Screen, textStyles, Title } from '@/components/ui';
import { getReceipt } from '@/services/storage';
import type { IncidentReceipt } from '@/types/domain';

export default function OccurrenceDetailScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const [receipt, setReceipt] = useState<IncidentReceipt | null>(null);
  const [state, setState] = useState<'loading' | 'ready' | 'error'>('loading');
  useEffect(() => {
    getReceipt(id).then((item) => { setReceipt(item); setState('ready'); }).catch(() => setState('error'));
  }, [id]);

  return <Screen>
    <Title eyebrow="DETALHE" subtitle="Comprovante local do seu registro.">Ocorrência</Title>
    {state === 'loading' ? <Card><ActivityIndicator color={colors.primary} /><Text style={textStyles.muted}>Carregando registro…</Text></Card> : null}
    {state === 'error' ? <Notice danger>Não foi possível abrir este registro salvo.</Notice> : null}
    {state === 'ready' && !receipt ? <Card><Text style={{ color: colors.ink, fontWeight: '800', fontSize: 18 }}>Registro não encontrado</Text><Text style={textStyles.muted}>Ele pode ter sido removido deste aparelho.</Text></Card> : null}
    {receipt ? <>
      <Card style={{ alignItems: 'center', paddingVertical: 26 }}><Text style={{ fontSize: 42 }}>✓</Text><Text style={{ color: colors.primary, fontWeight: '800', fontSize: 20 }}>Registro criado</Text><Text style={textStyles.muted}>Demonstração local</Text></Card>
      <Card>
        <Text style={textStyles.label}>Tipo</Text><Text style={textStyles.body}>{receipt.category}</Text>
        <Text style={textStyles.label}>Descrição</Text><Text style={textStyles.body}>{receipt.description}</Text>
        <Text style={textStyles.label}>Data e hora</Text><Text style={textStyles.body}>{new Date(receipt.createdAt).toLocaleString('pt-BR')}</Text>
        <Text style={textStyles.label}>Localização</Text><Text style={textStyles.body}>{receipt.locationIncluded ? 'GPS confirmado; coordenadas exatas não armazenadas.' : 'Não informada.'}</Text>
        <Text style={textStyles.label}>Situação</Text><Text style={textStyles.body}>{receipt.status}</Text>
      </Card>
    </> : null}
    <ActionButton label="Ver minhas ocorrências" secondary onPress={() => router.replace('/occurrences')} />
  </Screen>;
}
