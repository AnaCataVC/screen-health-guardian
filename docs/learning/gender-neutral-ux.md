# Gender-Neutral UX and Localization

## The Problem
Using direct translations or standard phrases in UX copy often unintentionally assumes the user's gender, which violates inclusive design principles. For example, using adjectives that modify the user directly in gendered languages like Spanish (e.g., "¡Siéntate derecho!", "Bienvenido", "Estás listo") alienates users who do not identify with the masculine form.

## The Solution
Adopt a strict gender-neutral writing pattern across all user-facing surfaces (Desktop App, Web, READMEs):
1. **Focus on the action or the object, not the subject**: Change "¡Siéntate derecho!" to "¡Mantén la espalda recta!" (Focuses on the back, which is a feminine noun "la espalda", bypassing the user's gender).
2. **Use passive or impersonal voice**: Instead of "Bienvenido", use "Te damos la bienvenida". 
3. **Refactor adjectives to nouns**: Instead of "tiempo activo", use "tiempo de actividad".

This has been codified as a global rule for the agent and should be maintained in all future i18n keys.
