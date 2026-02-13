### 2026-02-15: NoteEvent.Note must support non-string YAML constructs

**By:** Lemongrab
**What:** The `NoteEvent.Note` property is typed as `string`, but 2 of 5 example .sqnc.yaml files use `{ choice: [...] }` mapping constructs for note values (e.g., `note: { choice: [D2, A1] }`). These files (`another-brick-in-the-wall.sqnc.yaml`, `little-fluffy-clouds.sqnc.yaml`) fail to deserialize through `SequenceParser`. The `Note` field needs to become `object` or a union type that can represent both plain note names and choice/weighted-random constructs. Similarly, the `Pattern` field in `SequenceEntry` has the same issue with `{ choice: [...], weights: [...] }`.
**Why:** 40% of example files can't be parsed. This blocks any feature that needs to load those sequences. The model needs to evolve before M1 work can treat all example files as valid test fixtures. Filed as a known limitation with regression tests documenting the current failure.
