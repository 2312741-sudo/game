# DRINK-WAVE held prompts

Runtime/test head `07f6e73`, based on merged PR8 and PR9.

The prompt view renders the aimed station action and held-item action together, with independent progress and localized control glyphs. The generic held driver rejects unsupported Continuous actions explicitly. Frozen held/interaction interfaces, input asset, assemblies and package pins unchanged.

Unity6000.6.0f1 compile PASS, zero C# diagnostics; full EditMode112/112, PlayMode27/27 PASS; clean-checkout QA000 PASS. Includes2 new EditMode and2 PlayMode regressions, actual prompt UI and zero steady-frame allocations. Independent Claude review requested.
