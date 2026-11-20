import type { ReactNode } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View, type StyleProp, type ViewStyle } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

export const colors = {
  background: '#F3F7F5', surface: '#FFFFFF', ink: '#142B27', muted: '#657873',
  primary: '#116B58', primaryLight: '#DDF2E9', border: '#DCE7E2', danger: '#A23C32',
  warning: '#F4B544',
};

export function Screen({ children, contentStyle }: { children: ReactNode; contentStyle?: StyleProp<ViewStyle> }) {
  return <SafeAreaView style={styles.safe}><ScrollView contentContainerStyle={[styles.content, contentStyle]} keyboardShouldPersistTaps="handled">{children}</ScrollView></SafeAreaView>;
}

export function Title({ eyebrow, children, subtitle }: { eyebrow?: string; children: string; subtitle?: string }) {
  return <View style={styles.titleBlock}>{eyebrow ? <Text style={styles.eyebrow}>{eyebrow}</Text> : null}<Text style={styles.title}>{children}</Text>{subtitle ? <Text style={styles.subtitle}>{subtitle}</Text> : null}</View>;
}

export function Card({ children, style }: { children: ReactNode; style?: StyleProp<ViewStyle> }) {
  return <View style={[styles.card, style]}>{children}</View>;
}

export function ActionButton({ label, onPress, secondary = false, disabled = false }: { label: string; onPress: () => void; secondary?: boolean; disabled?: boolean }) {
  return <Pressable accessibilityRole="button" onPress={onPress} disabled={disabled} style={({ pressed }) => [styles.button, secondary && styles.buttonSecondary, disabled && styles.buttonDisabled, pressed && !disabled && styles.buttonPressed]}><Text style={[styles.buttonText, secondary && styles.buttonSecondaryText]}>{label}</Text></Pressable>;
}

export function Notice({ children, danger = false }: { children: string; danger?: boolean }) {
  return <Text accessibilityRole="alert" style={[styles.notice, danger && styles.noticeDanger]}>{children}</Text>;
}

export const textStyles = StyleSheet.create({ body: { color: colors.ink, fontSize: 16, lineHeight: 24 }, muted: { color: colors.muted, fontSize: 14, lineHeight: 21 }, label: { color: colors.ink, fontSize: 14, fontWeight: '700', marginBottom: 8 }, small: { color: colors.muted, fontSize: 12, lineHeight: 18 } });

const styles = StyleSheet.create({ safe: { flex: 1, backgroundColor: colors.background }, content: { paddingHorizontal: 22, paddingTop: 18, paddingBottom: 38, gap: 16 }, titleBlock: { gap: 6, marginBottom: 4 }, eyebrow: { color: colors.primary, fontSize: 12, fontWeight: '800', letterSpacing: 1.2, textTransform: 'uppercase' }, title: { color: colors.ink, fontSize: 28, fontWeight: '800', letterSpacing: -0.6 }, subtitle: { color: colors.muted, fontSize: 15, lineHeight: 22 }, card: { backgroundColor: colors.surface, borderColor: colors.border, borderWidth: 1, borderRadius: 20, padding: 18, gap: 10 }, button: { minHeight: 54, alignItems: 'center', justifyContent: 'center', borderRadius: 15, backgroundColor: colors.primary, paddingHorizontal: 18 }, buttonSecondary: { backgroundColor: colors.primaryLight }, buttonDisabled: { opacity: 0.5 }, buttonPressed: { opacity: 0.82 }, buttonText: { color: '#FFFFFF', fontWeight: '800', fontSize: 16 }, buttonSecondaryText: { color: colors.primary }, notice: { color: colors.primary, backgroundColor: colors.primaryLight, padding: 12, borderRadius: 12, fontSize: 14, lineHeight: 20 }, noticeDanger: { color: colors.danger, backgroundColor: '#FDEAE7' } });
