# Trønder Leikan - System Documentation

## Table of Contents

1. [Overview](#overview)
2. [Core Concepts](#core-concepts)
3. [Data Models](#data-models)
4. [Business Logic](#business-logic)
5. [Navigation & User Interface Hierarchy](#navigation--user-interface-hierarchy)
6. [Implementation Guidelines](#implementation-guidelines)

---

## Overview

### What is Trønder Leikan?

Trønder Leikan is a tournament management and scoring system designed to track gaming competitions within an organization. The system allows administrators to:

- Create and manage tournaments
- Organize multiple games within each tournament
- Track participant performance and rankings
- Award points based on a configurable scoring system
- Display real-time scoreboards
- Maintain player profiles and participation history

The platform serves both administrative users (who input data via a CMS) and public users (who view tournament results and player statistics).

---

## Core Concepts

### Tournament

A tournament is a collection of games played over a period. Each tournament:

- Has a unique name and identifier
- Contains multiple games
- Defines its own point scoring rules
- Maintains a scoreboard ranking all participants

### Game

A game is a single competitive event within a tournament where:

- Participants compete for placements (1st, 2nd, 3rd place)
- Organizers coordinate the event
- Spectators can watch without competing
- Results determine point allocations

### Person

A person represents any individual who can:

- Participate in games
- Organize games
- Spectate games
- Accumulate points across multiple tournaments

### Point System

Points are awarded based on various actions:

- Participating in a game
- Placing 1st, 2nd, or 3rd
- Organizing a game (with or without participation)
- Spectating a game

---

## Data Models

### 1. Person (Document)

**Purpose**: Represents an individual who can participate in, organize, or spectate games.

**Fields**:

- `firstName` (String, Required): Person's first name
- `lastName` (String, Required): Person's last name
- `image` (Image, Optional): Profile picture with hotspot capability for cropping

**Relationships**:

- Referenced by Game entities as participants, organizers, spectators, or placement winners

**Display**:

- Preview shows full name (firstName + lastName) and profile image

---

### 2. Tournament (Document)

**Purpose**: Container for a series of games with unified scoring rules.

**Fields**:

- `name` (String, Required): Tournament name
- `slug` (Slug, Required): URL-friendly identifier, auto-generated from name (max 96 characters)
- `games` (Array of Game Objects): Collection of games in this tournament
- `pointRules` (TournamentPointRules Object): Scoring configuration for this tournament

**Organizational Groups**:
The data is organized into three logical sections:

1. **Info**: Basic tournament information (name, slug)
2. **Games**: List of games in the tournament
3. **Point Rules**: Scoring configuration

---

### 3. Game (Object, not Document)

**Purpose**: Represents a single competitive event within a tournament.

**Fields**:

_Basic Information:_

- `name` (String, Required): Game name
- `description` (Text, Optional): Detailed description of the game
- `image` (Image, Optional): Game image with hotspot capability

_Organization:_

- `organiziers` (Array of Person References): People who organized this game
- `isOrganizersParticipating` (Boolean, Default: false): Whether organizers also participated as players

_Participation:_

- `participants` (Array of Person References): All people who played this game

_Results:_

- `isDone` (Boolean, Default: false): Whether results are finalized and ready for display
- `firstPlace` (Array of Person References): Winner(s) - supports ties
- `secondPlace` (Array of Person References): Second place finisher(s) - supports ties
- `thirdPlace` (Array of Person References): Third place finisher(s) - supports ties

_Spectators:_

- `spectators` (Array of Person References): People who watched but didn't participate

**Organizational Groups**:

1. **Info**: Basic game details
2. **Participants**: Player list
3. **Results**: Placement results
4. **Spectators**: Observer list

**Key Business Rules**:

- A game can have multiple people in any placement (supporting ties)
- Organizers may or may not participate as players
- Only games marked as `isDone` are included in scoreboard calculations

---

### 4. TournamentPointRules (Object, not Document)

**Purpose**: Defines the point allocation system for a tournament.

**Fields** (all Number, Required, with default values):

- `participation` (Default: 3): Points awarded just for participating in a game
- `firstPlace` (Default: 3): Additional points for winning (1st place)
- `secondPlace` (Default: 2): Additional points for 2nd place
- `thirdPlace` (Default: 1): Additional points for 3rd place
- `organizedWithParticipation` (Default: 1): Points for organizing a game while also playing
- `organizedWithoutParticipation` (Default: 3): Points for organizing a game without playing
- `spectator` (Default: 1): Points awarded for spectating

**Important Notes**:

- Placement points (1st, 2nd, 3rd) are **in addition to** participation points
- Example: A first-place winner with default rules gets participation (3) + firstPlace (3) = 6 total points
- Organizing points are mutually exclusive based on the `isOrganizersParticipating` flag

---

## Business Logic

### 1. Scoreboard Calculation

The scoreboard aggregates all completed games in a tournament and calculates each person's total score.

**Algorithm**:

For each completed game (where `isDone` is true):

1. **Participation Points**:
   - Award `participation` points to everyone in `participants` array
2. **Placement Points**:
   - Award `firstPlace` points to everyone in `firstPlace` array
   - Award `secondPlace` points to everyone in `secondPlace` array
   - Award `thirdPlace` points to everyone in `thirdPlace` array
3. **Organizer Points**:
   - If `isOrganizersParticipating` is true:
     - Award `organizedWithParticipation` points to each organizer
     - Also award `participation` points (they are counted as participants)
   - If `isOrganizersParticipating` is false:
     - Award `organizedWithoutParticipation` points to each organizer
     - Do NOT award participation points

4. **Spectator Points**:
   - Award `spectator` points to everyone in `spectators` array

**Scoring Formula for a Person**:

```
Total Score =
  (participations × participation) +
  (firstPlaces × firstPlace) +
  (secondPlaces × secondPlace) +
  (thirdPlaces × thirdPlace) +
  (organizedWithParticipations × organizedWithParticipation) +
  (organizedWithoutParticipations × organizedWithoutParticipation) +
  (spectatorCount × spectator)
```

**Ranking**:

- Players are sorted by total score (descending)
- Ties are handled: multiple players with the same score share the same rank
- Example: If two players tie for 2nd place with equal scores, both get rank 2, and the next player gets rank 4

---

### 2. Point System Rationale

The default point values reflect these principles:

- **Participation is valued**: Base 3 points encourages involvement
- **Winning matters**: Additional 3 points doubles your reward
- **Organization without playing is valuable**: 3 points recognizes pure organizing effort
- **Multiple roles stack**: An organizer who also plays and wins can accumulate multiple point categories
- **Spectating is acknowledged**: 1 point provides minor recognition for engagement

---

### 3. Player Statistics Tracking

For each player across all games in a tournament, the system tracks:

**Participation Metrics**:

- Total number of games participated in
- Total number of games spectated

**Performance Metrics**:

- Count of 1st place finishes
- Count of 2nd place finishes
- Count of 3rd place finishes

**Organization Metrics**:

- Count of games organized with participation
- Count of games organized without participation

**Aggregated Score**:

- Total points earned (calculated using the formula above)
- Rank position on the scoreboard

---

### 4. Player Profile History

When viewing a player's profile, the system shows:

**For Each Tournament**:

- Tournament name
- List of games the player participated in

**For Each Game Participation**:

- Game name
- Placement achieved (1st, 2nd, 3rd, or "Participated")
- Whether the player was an organizer

**Implementation Note**:

- Only games marked as `isDone` are included in player history
- Uses a database query that finds all tournaments referencing the player and filters to completed games

---

## Navigation & User Interface Hierarchy

### Site Structure

```
Home (/)
├── Tournaments List View
│   └── Individual Tournament View (/tournaments/{id})
│       ├── Point System Display
│       ├── Games List
│       │   └── Individual Game View (/tournaments/{id}/games/{key})
│       │       ├── Game Details
│       │       ├── Organizers List
│       │       ├── Results by Placement
│       │       ├── Participants List
│       │       └── Spectators List
│       └── Scoreboard Table
│
└── Players Section (/players)
    ├── All Players Grid View
    └── Individual Player Profile (/players/{id})
        ├── Player Photo
        └── Tournament Participation History
```

### Page Descriptions

#### 1. Home Page (/)

**Purpose**: Entry point listing all tournaments

**Content**:

- Welcome message
- Card grid of all tournaments
- Each card links to tournament detail page

---

#### 2. Tournament List Page (/tournaments)

**Behavior**: Redirects to home page (/) since tournaments are already shown there

---

#### 3. Tournament Detail Page (/tournaments/{id})

**Purpose**: Complete overview of a single tournament

**Content Sections** (in order):

1. **Tournament Name** (Heading)
2. **Point System Card**
   - Displays all point values for:
     - Participation
     - 1st/2nd/3rd Place
     - Organizing (with/without participation)
     - Spectating
3. **Games List**
   - Grid of game cards
   - Each shows: name, organizers, completion status (✅ if done)
   - Links to individual game detail pages
4. **Scoreboard Table**
   - Columns: Rank, Photo, Name, Participations, Spectator Count, Placements (1st/2nd/3rd), Organizer (With/Without), Total Points
   - Sorted by total points (descending)
   - On mobile: hides some columns for responsive design

---

#### 4. Game Detail Page (/tournaments/{id}/games/{key})

**Purpose**: Detailed view of a single game and its results

**Content Sections**:

1. **Back Navigation** (to tournament page)
2. **Game Name** (Heading)
3. **Game Image** (if available)
4. **Game Information**
   - Organizers (names and photos)
   - Description
5. **Results Cards** (in order):
   - **First Place** (gold/amber border) - shows point value and winners
   - **Second Place** (silver/slate border) - shows point value and runners-up
   - **Third Place** (bronze/amber-dark border) - shows point value and 3rd place
   - **Participants** - shows point value and all players
   - **Spectators** - shows point value and all watchers

---

#### 5. Players List Page (/players)

**Purpose**: Browse all registered players

**Content**:

- Back link to home
- Grid layout (responsive: 2/4/6 columns)
- Each player card shows:
  - Profile photo (grayscale, color on hover)
  - Full name
  - Links to player detail page

**Sorting**: Alphabetically by first name, then last name

---

#### 6. Player Detail Page (/players/{id})

**Purpose**: Individual player's participation history

**Content**:

1. **Back Navigation** (to players list)
2. **Player Name** (Heading)
3. **Profile Photo**
4. **Participation History**
   - Grouped by tournament
   - For each tournament:
     - Tournament name
     - List of games played (only completed games)
   - For each game:
     - Game name
     - Placement achieved
     - Whether they organized it

---

### Visual Design Patterns

**Theming**:

- Supports light and dark mode
- Theme toggle available site-wide

**Responsive Design**:

- Mobile-first approach
- Tables hide non-essential columns on small screens
- Grids adjust column count based on viewport

**Visual Feedback**:

- Completed games marked with ✅
- Placement cards use color-coded borders (gold/silver/bronze)
- Images use grayscale with hover effects
- Loading states with skeleton components

---

## Implementation Guidelines

### Technology-Agnostic Requirements

Any implementation of this system should provide:

#### 1. Data Storage

- **Document/Entity Storage**:
  - Person entities with profile information
  - Tournament entities with metadata
- **Relational Capabilities**:
  - Games linked to tournaments (one-to-many)
  - Games referencing persons (many-to-many)
  - Support for multiple references per role (participants, organizers, placements, spectators)

- **Image Handling**:
  - Store images with support for transforms/cropping (hotspot)
  - Generate URLs for web display

#### 2. Content Management

Provide an administrative interface to:

- Create/edit/delete persons
- Create/edit/delete tournaments
- Add games to tournaments
- Assign people to various game roles
- Mark games as complete
- Configure point rules per tournament

#### 3. Public API/Query Interface

Support queries for:

- List all tournaments
- Get tournament details with all games and populated person references
- List all persons
- Get person details with participation history across tournaments
- Filter games by completion status

#### 4. Business Logic Implementation

Implement the scoreboard calculation algorithm:

- Iterate through completed games
- Accumulate statistics per person
- Calculate total points using the formula
- Sort and rank players

#### 5. Frontend Display

Create views for:

- Tournament listing
- Tournament detail with scoreboard
- Game detail with results
- Player listing
- Player profile with history

### Data Validation Rules

**Person**:

- First name and last name required
- Image optional but recommended

**Tournament**:

- Name required
- Slug must be unique
- Point rules required with numeric values

**Game**:

- Name required
- Can have zero or more people in any role
- Placement arrays can have multiple people (supporting ties)
- `isDone` flag determines scoreboard inclusion
- If organizers participate, they must also be in participants array

**Point Rules**:

- All values must be numbers
- Negative values should be prevented (or allowed if subtracting points is desired)
- Default values provided for all fields

### Performance Considerations

**Caching Strategy**:

- Tournament lists: Low-frequency updates (can cache 1-5 minutes)
- Tournament details: Medium-frequency updates (cache 1 minute)
- Player lists: Low-frequency updates (can cache 1-5 minutes)
- Player details: Medium-frequency updates (cache 1 minute)

**Query Optimization**:

- Use reference expansion/joins to minimize database roundtrips
- Pre-calculate scoreboards if tournaments are large
- Index on common query fields (tournament ID, person ID, isDone flag)

### Localization Notes

The current implementation uses Norwegian language labels:

- "Turnering" = Tournament
- "Spill" = Game
- "Deltakere" = Participants
- "Tilskuere" = Spectators
- "Arrangører" = Organizers
- "Poengregler" = Point Rules

Any reimplementation should support internationalization.

---

## Appendix: Example Scenarios

### Scenario 1: Simple Game with Winner

**Setup**:

- 4 participants: Alice, Bob, Charlie, Diana
- Results: Alice 1st, Bob 2nd, Charlie 3rd, Diana participated
- Point rules: participation=3, 1st=3, 2nd=2, 3rd=1

**Points Awarded**:

- Alice: 3 (participation) + 3 (first) = 6
- Bob: 3 (participation) + 2 (second) = 5
- Charlie: 3 (participation) + 1 (third) = 4
- Diana: 3 (participation) = 3

### Scenario 2: Organizer Who Doesn't Play

**Setup**:

- Organizer: Eve (isOrganizersParticipating = false)
- 3 participants: Alice, Bob, Charlie
- Results: Alice 1st, Bob 2nd, Charlie 3rd
- Point rules: organizedWithoutParticipation=3

**Points Awarded**:

- Eve: 3 (organized without playing)
- Alice: 3 (participation) + 3 (first) = 6
- Bob: 3 (participation) + 2 (second) = 5
- Charlie: 3 (participation) + 1 (third) = 4

### Scenario 3: Tie for First Place

**Setup**:

- 4 participants: Alice, Bob, Charlie, Diana
- Results: Alice and Bob both 1st (tie), Charlie 3rd
- Point rules: participation=3, 1st=3, 3rd=1

**Points Awarded**:

- Alice: 3 (participation) + 3 (first) = 6
- Bob: 3 (participation) + 3 (first) = 6
- Charlie: 3 (participation) + 1 (third) = 4
- Diana: 3 (participation) = 3

**Scoreboard Ranking**:

1. Alice (6 points)
1. Bob (6 points) - same rank
1. Charlie (4 points) - rank 3, not 2
1. Diana (3 points)

### Scenario 4: Spectator

**Setup**:

- 3 participants: Alice, Bob, Charlie
- 1 spectator: Zara
- Results: Alice 1st
- Point rules: spectator=1

**Points Awarded**:

- Zara: 1 (spectator)
- Alice: 3 (participation) + 3 (first) = 6
- Bob: 3 (participation) = 3
- Charlie: 3 (participation) = 3

---

## Summary

Trønder Leikan is a comprehensive tournament tracking system with:

- **4 data models**: Person, Tournament, Game, TournamentPointRules
- **Flexible scoring**: Configurable per tournament
- **Multiple participation types**: Player, organizer, spectator
- **Rich statistics**: Player history and tournament scoreboards
- **User-friendly navigation**: Clear hierarchy from tournaments to games to players

The system is designed to be technology-agnostic, allowing reimplementation in any stack that supports document storage, references, and basic calculation logic.
