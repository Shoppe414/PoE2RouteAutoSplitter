# Authoritative PoE2 proper nouns

These resources are separate from the general SetupUI translation packs. The normal SetupUI build refreshes them from PoE2DB's current multilingual autocomplete data using `Refresh-ProperNouns.ps1`.

The refresh covers every non-English language in the current PoE2-supported language catalog: French, German, Spanish (Spain), Japanese, Korean, Portuguese (Brazil), Russian, and Thai. English remains the canonical source identity. SetupUI Language and PoE2 Game Language both use the same nine-language catalog.

The refresh downloads the autocomplete JSON as raw bytes and decodes it explicitly as strict UTF-8. This avoids Windows PowerShell 5.1 code-page decoding that can turn valid names into mojibake or corrupt CJK/Thai text.

The small checked-in JSON files are bootstrap/fallback samples so the source tree is self-contained. A normal build/run performs an online refresh and replaces them with all exact catalog matches it can verify. Bosses and other indexed proper nouns are resolved by the same PoE2DB entry path. Campaign and Atlas/map area names receive one additional authoritative-data fallback: if the autocomplete catalog does not expose the localized label, the refresh opens the same canonical PoE2DB area/map page and extracts its localized title. Synthetic or missing pages remain canonical English. The same resolved boss mapping also regenerates BossWatcher's localized boss-name database. Missing or ambiguous names are never machine-translated or guessed.

Area-page extraction prefers `og:title` and `<title>` metadata before HTML headings. Any candidate containing area-record metadata such as `Id:` or `Connections:` is rejected/cleaned so a nested PoE2DB details table cannot become a SetupUI display name. Route-only qualifiers such as `(blocked)` are removed before authoritative page lookup and re-applied after the canonical area name is resolved.
