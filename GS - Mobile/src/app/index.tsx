import { useEffect, useState } from 'react';
import { ActivityIndicator, Text, TextInput, View } from 'react-native';
import { router } from 'expo-router';

import { ActionButton, Card, colors, Notice, Screen, textStyles, Title } from '@/components/ui';
import { getTrafficSummary } from '@/services/api';
import { getIdentity, saveIdentity } from '@/services/storage';
import type { Identity, TrafficSummary } from '@/types/domain';
import { validateDisplayName } from '@/utils/validation';

const DEMO_ROUTES = [
  { name: 'Corredor Norte', status: 'Fluxo moderado', speed: '24 km/h', color: '#E6A52E', segments: [0.45, 0.8, 0.55, 0.35] },
  { name: 'Eixo Central', status: 'Fluxo intenso', speed: '11 km/h', color: '#D75C50', segments: [0.85, 0.42, 0.3, 0.7] },
  { name: 'Marginal Sul', status: 'Fluxo livre', speed: '38 km/h', color: '#3E9B72', segments: [0.35, 0.65, 0.9, 0.6] },
];

export default function HomeScreen() {
  const [identity, setIdentity] = useState<Identity | null>(null);
  const [identityReady, setIdentityReady] = useState(false);
  const [name, setName] = useState('');
  const [formError, setFormError] = useState('');
  const [summary, setSummary] = useState<TrafficSummary | null>(null);
  const [trafficState, setTrafficState] = useState<'loading' | 'ready' | 'empty' | 'error'>('loading');
  const [demoEnabled, setDemoEnabled] = useState(false);

  useEffect(() => {
    getIdentity().then((saved) => { setIdentity(saved); setIdentityReady(true); }).catch(() => setIdentityReady(true));
    void loadTraffic();
  }, []);

  async function loadTraffic() {
    setTrafficState('loading');
    try {
      const data = await getTrafficSummary();
      setSummary(data);
      setTrafficState(data ? 'ready' : 'empty');
    } catch {
      setTrafficState('error');
    }
  }

  async function continueWithName() {
    const validation = validateDisplayName(name);
    if (!validation.valid) { setFormError(validation.message); return; }
    const next = { displayName: name.trim() };
    try {
      await saveIdentity(next);
      setIdentity(next);
      setFormError('');
    } catch {
      setFormError('Não foi possível salvar seu nome neste aparelho. Tente novamente.');
    }
  }

  if (!identityReady) return <Screen contentStyle={{ flexGrow: 1, justifyContent: 'center', alignItems: 'center' }}><ActivityIndicator color={colors.primary} /><Text style={textStyles.muted}>Carregando seu perfil…</Text></Screen>;

  if (!identity) return <Screen contentStyle={{ flexGrow: 1, justifyContent: 'center' }}>
    <Text style={{ fontSize: 44 }}>🚦</Text>
    <Title eyebrow="CitizenTech · FIAP" subtitle="Participe de uma mobilidade urbana mais segura. Seu nome fica salvo neste aparelho.">Vamos começar</Title>
    <Card>
      <Text style={textStyles.label}>Como podemos chamar você?</Text>
      <TextInput accessibilityLabel="Seu nome" autoCapitalize="words" maxLength={40} onChangeText={setName} placeholder="Seu nome" placeholderTextColor={colors.muted} style={{ backgroundColor: '#F3F7F5', borderRadius: 12, padding: 15, color: colors.ink, fontSize: 16 }} value={name} />
      {formError ? <Notice danger>{formError}</Notice> : null}
      <ActionButton label="Entrar no app" onPress={() => void continueWithName()} />
    </Card>
  </Screen>;

  return <Screen>
    <View style={{ gap: 8 }}>
      <Text style={{ color: colors.primary, fontSize: 13, fontWeight: '800', letterSpacing: 1 }}>CITIZENTECH · MOBILIDADE</Text>
      <Text style={{ color: colors.ink, fontSize: 31, fontWeight: '800', letterSpacing: -0.8 }}>Olá, {identity.displayName} 👋</Text>
      <Text style={textStyles.muted}>Ajude a deixar o trânsito da cidade mais seguro.</Text>
    </View>
    <Card style={{ backgroundColor: '#E0F2EA', borderColor: '#C3E3D6' }}>
      <Text style={{ fontSize: 27 }}>📍</Text>
      <Text style={{ color: colors.ink, fontWeight: '800', fontSize: 20 }}>Viu algo no caminho?</Text>
      <Text style={textStyles.muted}>Registre uma ocorrência para apoiar o monitoramento urbano.</Text>
      <ActionButton label="Registrar ocorrência" onPress={() => router.push('/new-report')} />
    </Card>
    <View style={{ flexDirection: 'row', gap: 10 }}>
      <View style={{ flex: 1 }}><ActionButton label="Minhas ocorrências" secondary onPress={() => router.push('/occurrences')} /></View>
    </View>
    <Card>
      <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' }}>
        <View>
          <Text style={{ color: colors.ink, fontWeight: '800', fontSize: 18 }}>Trânsito agora</Text>
          <Text style={textStyles.small}>Visão esquemática da cidade</Text>
        </View>
        <View style={{ backgroundColor: demoEnabled ? '#FFF2D9' : colors.primaryLight, paddingHorizontal: 10, paddingVertical: 6, borderRadius: 30 }}>
          <Text style={{ color: demoEnabled ? '#815A12' : colors.primary, fontSize: 11, fontWeight: '800' }}>{demoEnabled ? 'DEMONSTRAÇÃO' : trafficState === 'ready' ? 'CENÁRIO FIAP' : 'MAPA'}</Text>
        </View>
      </View>
      <TrafficMap />
      {trafficState === 'loading' ? <View style={{ flexDirection: 'row', alignItems: 'center', gap: 8 }}><ActivityIndicator size="small" color={colors.primary} /><Text style={textStyles.small}>Consultando cenário…</Text></View> : null}
      {demoEnabled ? <>
        <View style={{ gap: 10 }}>
          {DEMO_ROUTES.map((route) => <View key={route.name} style={{ gap: 6 }}>
            <View style={{ flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' }}>
              <View style={{ flexDirection: 'row', alignItems: 'center', gap: 8, flex: 1 }}>
                <View style={{ width: 9, height: 9, borderRadius: 5, backgroundColor: route.color }} />
                <Text style={{ color: colors.ink, fontSize: 13, fontWeight: '700' }}>{route.name}</Text>
              </View>
              <Text style={{ color: route.color, fontSize: 12, fontWeight: '800' }}>{route.speed}</Text>
            </View>
            <View style={{ flexDirection: 'row', height: 7, gap: 3, borderRadius: 8, overflow: 'hidden' }}>
              {route.segments.map((intensity, index) => <View key={`${route.name}-${index}`} style={{ flex: 1, backgroundColor: route.color, opacity: intensity, borderRadius: 5 }} />)}
            </View>
            <Text style={textStyles.small}>{route.status}</Text>
          </View>)}
        </View>
        <Text style={[textStyles.small, { textAlign: 'center' }]}>Valores fictícios para demonstração; não representam o trânsito atual.</Text>
      </> : trafficState === 'ready' && summary ? <>
        <Text style={textStyles.muted}>{summary.environment} · Atualizado em {new Date(summary.asOfUtc).toLocaleString('pt-BR')}</Text>
        {summary.metrics.map((metric) => <View key={metric.label} style={{ flexDirection: 'row', justifyContent: 'space-between', borderTopColor: colors.border, borderTopWidth: 1, paddingTop: 8 }}><Text style={textStyles.body}>{metric.label}</Text><Text style={{ color: colors.primary, fontWeight: '800' }}>{metric.value}{metric.unit ? ` ${metric.unit}` : ''}</Text></View>)}
        {summary.limitations.length > 0 ? <Text style={textStyles.small}>{summary.limitations.join(' ')}</Text> : null}
      </> : <Text style={textStyles.small}>Ative a demonstração para visualizar indicadores ilustrativos.</Text>}
      {(trafficState === 'empty' || trafficState === 'error') && !demoEnabled ? <ActionButton label="Ativar modo demonstração" onPress={() => setDemoEnabled(true)} /> : null}
      {demoEnabled ? <ActionButton label="Desativar modo demonstração" secondary onPress={() => setDemoEnabled(false)} /> : null}
    </Card>
    <Text style={[textStyles.small, { textAlign: 'center' }]}>Projeto acadêmico FIAP · CitizenTech</Text>
  </Screen>;
}

function TrafficMap() {
  return <View accessibilityLabel="Mapa esquemático de vias" style={{ height: 112, borderRadius: 16, backgroundColor: '#E8F0EC', overflow: 'hidden', position: 'relative' }}>
    <View style={{ position: 'absolute', top: 45, left: -12, right: -12, height: 24, backgroundColor: '#CCD9D3', transform: [{ rotate: '-8deg' }] }} />
    <View style={{ position: 'absolute', top: -24, bottom: -24, left: '38%', width: 21, backgroundColor: '#CCD9D3', transform: [{ rotate: '12deg' }] }} />
    <View style={{ position: 'absolute', top: -20, bottom: -20, left: '74%', width: 17, backgroundColor: '#D6E1DC', transform: [{ rotate: '-18deg' }] }} />
    <View style={{ position: 'absolute', top: 49, left: 14, width: '27%', height: 7, borderRadius: 5, backgroundColor: '#3E9B72' }} />
    <View style={{ position: 'absolute', top: 48, left: '43%', width: '24%', height: 8, borderRadius: 5, backgroundColor: '#E6A52E' }} />
    <View style={{ position: 'absolute', top: 47, right: 13, width: '23%', height: 9, borderRadius: 5, backgroundColor: '#D75C50' }} />
    <View style={{ position: 'absolute', top: 10, left: 12, backgroundColor: '#FFFFFFD9', paddingHorizontal: 8, paddingVertical: 4, borderRadius: 8 }}><Text style={{ color: colors.ink, fontSize: 10, fontWeight: '800' }}>ZONA NORTE</Text></View>
    <View style={{ position: 'absolute', bottom: 9, right: 10, backgroundColor: '#FFFFFFD9', paddingHorizontal: 8, paddingVertical: 4, borderRadius: 8 }}><Text style={{ color: colors.ink, fontSize: 10, fontWeight: '800' }}>CENTRO</Text></View>
    <View style={{ position: 'absolute', top: 41, left: '37%', width: 16, height: 16, borderRadius: 10, backgroundColor: '#FFFFFF', borderColor: colors.primary, borderWidth: 4 }} />
  </View>;
}
