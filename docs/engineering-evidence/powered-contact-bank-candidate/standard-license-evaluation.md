# Direct standard-license evaluation

Inspected authoritative texts on 2026-09-15. This is a suitability analysis for
review, not legal advice or installation of any license. No standard text was
modified, renamed or copied into the candidate.

| Model / authoritative source | Grants and limits | Fit / classification |
| --- | --- | --- |
| [PolyForm Strict 1.0.0](https://polyformproject.org/licenses/strict/1.0.0) | Noncommercial use; no software modification or distribution grant. Includes a patent grant and specified breach procedure. | REQUIRES SEPARATE EXPRESS GRANTS / POLICIES. Alone it cannot satisfy private modification, supported mods and monetized creator use. |
| [PolyForm Noncommercial 1.0.0](https://polyformproject.org/licenses/noncommercial/1.0.0) | Noncommercial use plus modification and redistribution, with notice obligations. | NOT COMPATIBLE WITH PROJECT CONTROL INTENT as universal root: grants noncommercial core distribution that the requested model reserves; creator monetization also needs express treatment. |
| [MIT](https://opensource.org/license/mit) | Broad modification, distribution, sublicensing and sale permissions, subject to notice terms. | SUITABLE ONLY FOR SPECIFIC COMPONENT where its owner chooses it; NOT compatible with reserving NovaCore framework commercialization. |
| [Apache-2.0](https://www.apache.org/licenses/LICENSE-2.0) | Broad copyright/patent permissions, derivative distribution and notice requirements. | SUITABLE ONLY FOR SPECIFIC COMPONENT here: BEPU remains Apache-2.0. A universal NovaCore application would not reserve the requested commercial framework rights. |

PolyForm Strict's actual text has no Noncommercial-style distribution Required
Notice mechanism. Do not invent one or use a familiar notice prefix as a custom
rights switch. The official [license repository](https://github.com/polyformproject/polyform-licenses)
also requires removal of its branding if its text is changed. No altered standard
is being proposed.

## Model decision

| Option | Rights / creator / mod fit | Redistribution / commercial control | Complexity / maintenance / third parties |
| --- | --- | --- | --- |
| A: Strict alone | Personal use only; insufficient for approved modification/mod/media model | Reserved core distribution, but too restrictive for requested grants | Standard maintenance, clear independent third-party scope; incomplete product fit |
| B: Strict plus express grants | Could add private changes, mods and creator monetization | Must carefully override noncommercial/derivative limits without enabling core redistribution | Multiple interacting grants and precedence; legal review essential; do not edit Strict itself |
| C: Bespoke root with explanatory companions | Directly states personal modification, free original mods and monetized creator rights | Explicitly reserves public core forks, sale, paid mods and commercial embedding | More drafting/review burden; one controlling grant avoids scattered overrides; independently licensed components expressly excluded |

**Recommend C**, the concrete `docs/legal/ROOT-LICENSE-CANDIDATE.md`, with its
version-matched explanatory companions. This is one coherent model for review,
not three unresolved recommendations. Professional legal review is recommended
before adoption; no enforceability or legal clearance is claimed.
