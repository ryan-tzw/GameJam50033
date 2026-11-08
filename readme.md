# Neverland

**50.033 Midterm Game Jam: The Remix**

- **Team Members:** Ryan
- **Game Title:** Neverland
- **Primary Song / Artist:** Never Gonna Give You Up — Rick Astley
- **Remix Track / Artist:** Toxic — Britney Spears

## 1. Theme Interpretation (25%)

**Interpretation:** While *Never Gonna Give You Up* is commonly associated with Rickrolling or romantic devotion, I interpreted the lyric "Never gonna let you down" both literally (preventing someone from falling) and figuratively (not disappointing someone who depends on you).

The title *Neverland* is a **double entendre**: it references Peter Pan's fictional island, an unreachable destination representing the endless nature of the game, while also meaning "never landing" in the ocean below.

**Implementation:** Following the game jam's overarching theme of "Remix", I combined classic Tetris with an endless runner. Rather than controlling the adventurer directly, players construct an endless bridge using tetrominoes to support his journey across the ocean while the bridge continuously collapses behind him.

## 2. Gameplay & Player Experience (30%)

**Objective:** Keep the adventurer alive for as long as possible while accumulating points through distance travelled, milestones, coins, and treasure. There is no fixed victory condition; the run ends when the adventurer falls into the ocean or encounters a lethal hazard.

**Core Mechanics:** Players manipulate incoming tetrominoes by moving, rotating, holding, and hard-dropping them to construct a continuous bridge. Pieces must connect to existing terrain without overlapping occupied cells or entering the No-Build Zone, while the Entry Zone allows incoming pieces to ignore natural structures for placement calculations.

**Fun Factor:** The game combines the spatial reasoning of Tetris with the urgency of an endless runner, requiring players to construct a safe path under increasing time pressure. Players must balance the immediate need to extend the bridge against the risks of hazardous pieces, unfavorable shapes, and opportunities to collect rewards.

### Scoring

- **Distance:** 20 points per tile travelled.
- **Coins:** 50 points each.
- **Gems:** 150 points each.
- **Treasure Chests:** Random assortments of coins and gems.
- **Milestones:** Bonus points awarded at recurring distance milestones, with higher milestone tiers providing larger rewards.

Placing tetrominoes does not directly award points.

## 3. Remix Integration (20%)

**Interpretation:** The remix track, *Toxic* by Britney Spears, comes with the assigned constraint that something beneficial must also have an undesirable consequence. I incorporated this constraint into bridge construction, making an action essential for survival potentially dangerous.

**Implementation:** Tetrominoes can contain hazardous blocks, meaning that placing a piece to extend the bridge may simultaneously introduce threats to the adventurer. As the run progresses, hazardous pieces become more common.

**Interaction:** The remix complicates the original objective of maintaining a traversable bridge. Players must consider both the immediate benefit of extending the path and the potential consequences of introducing hazards, creating a trade-off between construction efficiency and safety.

**Demonstration:** The grader can place a hazard-bearing tetromino along the adventurer's path and observe how a placement that successfully extends the bridge can also threaten the adventurer's survival.

## 4. Technical Execution (15%)

**How to Run:**

Binaries can be found in the `Build/` folder.

**Controls:**

| Input | Action |
|---|---|
| ↑ | Move / attach piece upward |
| ↓ | Move / attach piece downward |
| ← | Soft drop (accelerate piece) |
| → | Stop falling |
| Z / X | Rotate piece counterclockwise / clockwise |
| Space | Hard drop |
| C | Hold / swap piece |
| Esc | Pause game |

**Known Bugs / Limitations:** None reported.

## 5. Game Design & Balance (10%)

**Challenge:** Players must maintain a safe, continuous bridge while adapting to unpredictable tetromino shapes, environmental hazards, and the adventurer's autonomous movement. Difficulty increases over time through faster scrolling and a higher probability of hazardous pieces, requiring increasingly rapid decisions.

**Balance:** Quick placements can help maintain forward progress but may introduce dangerous terrain or unfavorable structures. Players can hold an undesirable piece for later use, but the hold action is restricted until the active piece is placed, preventing unrestricted swapping.

**Demonstration:** The grader should construct a bridge using hazardous tetrominoes while the adventurer approaches, then continue playing as the game's difficulty increases. This demonstrates the trade-off between rapid construction and safety, as well as the importance of managing unfavorable pieces.

## 6. Credits (Mandatory)

(I may have missed some stuff)

### Background Music (BGM)

- **Game Over Music:** [Lighthouse](https://crossedkiller.itch.io/lighthouse-free-nostalgia-music) — Crossedkiller.
- **Gameplay Music:** *Patreon Goal Reward Loops – Track 08* — [Abstraction](http://www.abstractionmusic.com/).
- **Main Menu Music:** [Arcade Puzzler (Looping)](https://opengameart.org/content/arcade-puzzler-looping) — Eric Matyas ([Soundimage](https://soundimage.org/)).

### Sound Effects (SFX)

- **Tetromino Sound Effects:** [Tetris 99 – General Sounds](https://sounds.spriters-resource.com/nintendo_switch/tetris99/asset/412904/) — *Tetris 99*, via The Sounds Resource.
- **Player Movement:** [12 Player Movement SFX](https://opengameart.org/node/139406) — leohpaz, OpenGameArt.
- **UI Interaction Sounds:** [Interface Sounds](https://kenney.nl/assets/interface-sounds) — Kenney.
- **Item Pickup:** [Coin Collect Retro 8-bit Sound Effect](https://pixabay.com/sound-effects/film-special-effects-coin-collect-retro-8-bit-sound-effect-145251/) — Lesiakower, Pixabay.
- **Coin Collection:** [Nintendo Coin Pickup](https://freesound.org/people/plasterbrain/sounds/608433/) — plasterbrain, Freesound.

### Sprites & Textures

- **Player Sprite:** [Mini Legend – Asset Pack](https://alwaysrice.itch.io/mini-legend) — AlwaysRice.
- **Brick Textures:** [Pixel Platformer Blocks](https://kenney.nl/assets/pixel-platformer-blocks) — Kenney.
- **Game Icons:** [Game Icons](https://kenney.nl/assets/game-icons) — Kenney.
- **Control Icons:** [Input Prompts](https://kenney.nl/assets/input-prompts) — Kenney.

### UI & Typography

- **Fraunces:** [Google Fonts – Fraunces](https://fonts.google.com/specimen/Fraunces).
- **Fredoka:** [Google Fonts – Fredoka](https://fonts.google.com/specimen/Fredoka).

### Development Tools

- **Game Engine:** Unity 6.3.
- **Unity Packages:** Universal Render Pipeline (URP), Input System, uGUI.

### AI Tools

- **ChatGPT:** Used as a search engine for finding assets, generated the parallax background image, code generation, and documentation.

---

**Repository:** [ryan-tzw/GameJam50033](https://github.com/ryan-tzw/GameJam50033)