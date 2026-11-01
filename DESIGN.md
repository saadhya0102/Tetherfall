---
name: Project Tetherfall
description: Field-worn fantasy interfaces lit by copper routework and Threadglass teal
colors:
  deep-slate: "#0D1C24"
  routework-copper: "#C47838"
  threadglass-teal: "#4AC9BD"
  warm-parchment: "#F0E0BA"
  clear-mist: "#D6E8E6"
  muted-reed: "#96B5B8"
  ink: "#03080A"
  danger-cinder: "#C74538"
typography:
  title:
    fontFamily: "Segoe UI, Arial, sans-serif"
    fontSize: "17px"
    fontWeight: 700
  body:
    fontFamily: "Segoe UI, Arial, sans-serif"
    fontSize: "13px"
    fontWeight: 400
  label:
    fontFamily: "Segoe UI, Arial, sans-serif"
    fontSize: "10px"
    fontWeight: 400
rounded:
  panel: "0px"
spacing:
  xs: "8px"
  sm: "14px"
  md: "20px"
  lg: "26px"
components:
  field-panel:
    backgroundColor: "{colors.deep-slate}"
    textColor: "{colors.clear-mist}"
    rounded: "{rounded.panel}"
    padding: "{spacing.sm}"
  interaction-prompt:
    backgroundColor: "{colors.ink}"
    textColor: "{colors.warm-parchment}"
    rounded: "{rounded.panel}"
    padding: "{spacing.xs}"
---

# Design System: Project Tetherfall

## Overview

**Creative North Star: "The Wayfinder's Field Instrument"**

The interface should feel built for an expedition, not layered over one. Dense slate surfaces, copper rules, pale field text, and sparse Threadglass teal make every panel read like a durable route tool. The presentation is restrained and practical while the world carries the brighter regional color.

**Key Characteristics:**
- Dark translucent field panels with sharp corners
- Copper used for structure and emphasis
- Teal reserved for Threadglass energy and successful route states
- Compact information density with plain, direct language

## Colors

The palette balances cool, low-light navigation surfaces with warm metal accents.

### Primary
- **Routework Copper:** structural rules, rare emphasis, and crafted machinery.
- **Threadglass Teal:** abilities, restored routes, active resonance, and positive state.

### Secondary
- **Danger Cinder:** health loss, hostile telegraphs, and critical warnings.

### Neutral
- **Deep Slate:** the standard HUD and modal surface.
- **Warm Parchment:** headings and high-priority prompt text.
- **Clear Mist:** ordinary readable body copy.
- **Muted Reed:** supporting text and secondary state.
- **Ink:** prompt backing and bar tracks.

**The Signal Rarity Rule.** Copper and teal are signals, not wallpaper. Keep most interface area in slate and ink.

## Typography

**Display Font:** Segoe UI with Arial fallback  
**Body Font:** Segoe UI with Arial fallback

The type system is compact, readable, and intentionally non-decorative so the fiction comes from names, materials, and color instead of ornamental fonts.

### Hierarchy
- **Title:** bold 17px text for panel headings and major states.
- **Body:** 13px wrapped text for objectives, descriptions, and instructions.
- **Prompt:** bold 15px centered text for nearby interactions.
- **Muted:** 11px text for costs, status, and support detail.
- **Label:** 10px text for persistent controls and panel identifiers.

## Layout

Persistent HUD modules anchor to screen corners while interaction prompts and boss state remain centered. Modal panels use one large central field surface with generous outer margins and compact internal rows. Spacing follows an 8px to 26px rhythm. Information should remain readable at 1280x720 and above without covering the player focus area.

## Elevation & Depth

The interface uses tonal layering rather than shadows. Translucent slate sits over darker ink and a near-black modal scrim. Thin copper top rules separate panels from the world. World depth comes from fog, lighting, overlapping low-poly silhouettes, and emissive route markers.

**The Flat Fieldwork Rule.** UI surfaces stay flat. Depth belongs to the game world, not floating card stacks.

## Shapes

Panels and prompts use sharp rectangular silhouettes. World markers use circles, cylinders, arches, and repeated Threadglass rings. Organic terrain and vegetation should contrast with geometric routework, pylons, gates, and crafting stations.

## Components

### Field panels
- Use deep slate at high opacity.
- Add one thin copper rule across the top.
- Keep labels inset by 14px or more.

### Interaction prompts
- Center above the control footer.
- Use an ink backing with warm parchment text.
- Start with the exact input key, then the action.

### Bars
- Use ink as the track.
- Vitality uses danger cinder; resonance and successful progress use Threadglass teal.
- Keep bars thin and pair them with a text value when precision matters.

### World labels
- Use warm parchment for names and cool muted teal for roles or categories.
- Keep labels short enough to read while moving.

### Hostile field instrument
- One central threat director may show at most four combatants, ordered by Threadglass focus, recent damage, then awake hostiles within 18 metres.
- Threat plates use slate and ink, a copper top rule, parchment names, cinder health, and a thin teal poise measure. Teal also marks the current Threadglass focus.
- Keep archetype shapes and text together. Status priority is ASHGUARD, STAGGERED, then EXPOSED.
- Bosses use one centered strip with phase, threat band, numeric vitality, poise, and current status.
- Hostile outlines use pooled low-poly proxy hulls. Focus is reinforced by greater thickness and scale so grayscale and color-vision variants do not rely on hue.

## Do's and Don'ts

### Do:
- **Do** keep routework interfaces compact, sharp, and legible.
- **Do** use copper for structure and teal for state changes.
- **Do** let each region carry a distinct environmental tint while preserving common routework materials.
- **Do** write controls and objectives as direct actions.

### Don't:
- **Don't** use glossy generic fantasy frames or ornate medieval decoration.
- **Don't** fill large surfaces with bright copper or teal.
- **Don't** obscure telegraphs with decorative effects.
- **Don't** use marketing language inside gameplay interfaces.
