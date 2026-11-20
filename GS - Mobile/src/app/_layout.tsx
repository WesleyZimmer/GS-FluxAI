import { DarkTheme, DefaultTheme, Stack, ThemeProvider } from 'expo-router';
import { useColorScheme } from 'react-native';

import { colors } from '@/components/ui';

export default function RootLayout() {
  const colorScheme = useColorScheme();
  return (
    <ThemeProvider value={colorScheme === 'dark' ? DarkTheme : DefaultTheme}>
      <Stack screenOptions={{ headerStyle: { backgroundColor: colors.background }, headerTintColor: colors.ink, headerTitleStyle: { fontWeight: '700' }, contentStyle: { backgroundColor: colors.background }, headerShadowVisible: false }}>
        <Stack.Screen name="index" options={{ title: 'CitizenTech', headerShown: false }} />
        <Stack.Screen name="occurrences" options={{ title: 'Minhas ocorrências' }} />
        <Stack.Screen name="new-report" options={{ title: 'Nova ocorrência' }} />
        <Stack.Screen name="occurrences/[id]" options={{ title: 'Detalhes' }} />
      </Stack>
    </ThemeProvider>
  );
}
