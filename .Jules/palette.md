## 2025-01-24 - [Accessibility in Avalonia]
**Learning:** Explicitly disabling `FocusAdorner` for all buttons in a global style without providing a clear alternative focus state (like a high-contrast border or background change) breaks keyboard navigation accessibility. Adding `AutomationProperties.Name` to icon-only buttons or buttons with symbolic content (like ◀/▶) is essential for screen reader support in Avalonia.
**Action:** When working with Avalonia, ensure every interactive element has a perceptible focus state and descriptive `AutomationProperties.Name` if its purpose isn't clear from text content alone.

## 2025-01-24 - [Accessible Slots and Markers]
**Learning:** Interactive grid elements (like Pokemon slots) and symbolic checkboxes (like markings) are often opaque to screen readers if they only contain images or symbols. Binding `AutomationProperties.Name` to a text summary (like `ToolTipSummary`) or providing explicit labels for symbols ensures the UI remains functional for all users.
**Action:** Always bind `AutomationProperties.Name` for repetitive interactive elements that represent complex data objects.

## 2025-01-25 - [Cohesive Accessibility Labeling]
**Learning:** In complex editor UIs with many unlabeled inputs (like stats or IDs), adding `AutomationProperties.Name` provides a massive accessibility boost with zero visual impact. For grid-based data like Pokémon slots, reusing existing tooltip summary properties for `AutomationProperties.Name` ensures consistency between visual and screen reader feedback.
**Action:** Always check symbol-based controls (like markings) and input-dense grids for missing programmatic labels.

## 2025-02-12 - [Contextual Accessibility Labels]
**Learning:** For dynamic elements like the Pokémon Sprite, binding `AutomationProperties.Name` to a summary property (like `Title`) provides much better context than a static label. Similarly, providing tooltips for ambiguous symbolic indicators (like a red warning triangle for "Illegal") is crucial for both screen reader and visual accessibility.
**Action:** Use existing descriptive ViewModel properties (e.g., `Title`, `DisplayName`) for `AutomationProperties.Name` on images and complex controls.
