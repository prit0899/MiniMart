# My Mini Mall Tycoon – Single Source of Truth (plan.md)

This document is the definitive plan for the UNITY game. It specifies the world layout, characters and roles, production chains, shops/storage locations, upgrade paths, and core systems. Use it as the single source of truth for design, implementation, and balancing.

## 1) High-Level Vision

- Genre: Casual tycoon/idle management with light farming, processing, and retail.
- Loop:
  1) Grow/raise resources (agriculture + livestock)
  2) Process goods in the factory
  3) Stock shelves and sell to customers in the supermarket
  4) Reinvest profits in upgrades, expansions, and staff
- Player Fantasy: Build and run a lively mini-mall with a farm-to-shelf pipeline.

## 2) World Map Layout

The map is divided into five horizontal zones from south to north (bottom to top on screen). Left and right are for expansion and optional side areas.

- Livestock West (bottom-left quadrant)
  - Chicken coop (eggs)
  - Cow pen (milk)
  - Feed troughs and water troughs
  - Barn storage (raw livestock outputs)
- Agriculture East (bottom-right quadrant)
  - Fields for crops: wheat, corn, tomatoes, apples (expandable plots)
  - Seed bin and fertilizer station (upgrades unlock)
  - Sheds for raw crop storage
- The Supply & Farm (central grass strip)
  - Player spawn area and interaction ring
  - Pallet spots for intermediate items
  - Forklift/handcart spawn points (if unlocked)
  - Assistant nodes (automation waypoints)
- Processing Area (central strip, slightly above grass)
  - Factory building with modular machines:
    - Tomato canner (Tomatoes -> Canned Tomatoes)
    - Corn processor (Corn -> Processed Corn)
    - Dough mixer + oven (Wheat -> Bread)
    - Milk bottler (Milk -> Bottled Milk)
    - Cookie line (Wheat + Milk -> Cookies)
  - Input/Output pallets for each machine
  - Power/fuel node (for future systems)
- The Supermarket Zone (top band)
  - Bakery & Café (pink floor, right)
    - Bread shelves, cookie display, coffee machine (future)
  - Central Display (center)
    - Produce tables: apples, tomatoes, corn
    - Canned goods and processed items
  - Cashier Section (left)
    - Checkout counters (expandable)
    - Basket stands, customer queue markers
  - Backroom storage (behind shelves)
    - Cold storage (milk, future cold items)
    - Dry storage (bread, canned goods, cookies)
    - Staff room (speed/efficiency buffs, future)

Entrances/Exits:
- Customer entrance: Top-left door near cashier section.
- Delivery bay: Mid-left edge near processing area (for future delivery mechanics).
- Player start: Central grass strip.

## 3) Coordinates & Placement (Unity Scene)

Use a single root object “WorldRoot” with child containers:
- WorldRoot/Zone_LivestockWest
- WorldRoot/Zone_AgricultureEast
- WorldRoot/Zone_SupplyGrass
- WorldRoot/Zone_Processing
- WorldRoot/Zone_Supermarket

Relative grid (Unity units; 1u = 1m; Y-up, Z-forward for 3D; X-right):
- Zone_Supermarket: Z = 40 to 60
- Zone_Processing: Z = 28 to 38
- Zone_SupplyGrass: Z = 18 to 26
- Zone_LivestockWest: X = -30 to -5, Z = 4 to 16
- Zone_AgricultureEast: X = 8 to 32, Z = 4 to 16

Key anchors (approximate centers):
- Cashier section center: (-10, 0, 50)
- Central display center: (0, 0, 50)
- Bakery center: (12, 0, 50)
- Factory center: (0, 0, 32)
- Player spawn: (0, 0, 20)
- Chicken coop: (-20, 0, 10)
- Cow pen: (-12, 0, 10)
- Wheat field: (16, 0, 8)
- Corn field: (24, 0, 8)
- Tomato patch: (16, 0, 12)
- Apple trees: (24, 0, 12)

Note: Fine-tune based on art scale; maintain clear walking paths and conveyor/pallet access.

## 4) Characters and Roles

- Player (Blue Character)
  - Movement: WASD/Joystick; tap-to-move on mobile
  - Interacts with: harvest nodes, machine inputs, shelves, cash registers (emergency)
  - Carry capacity: base 5 units, upgradable
- Customers (Pink/Green NPCs)
  - Behavior: Enter -> browse -> pick items -> queue -> checkout -> exit
  - Needs: Variety, stock availability, queue speed
  - Mood: Affects patience and spending
- Workers (Assistants)
  - Types:
    - Stocker: Moves processed goods from backroom/pallets to shelves
    - Harvester: Collects from fields/livestock and deposits to processing inputs
    - Cashier: Operates a checkout counter
  - AI: Waypoint-based tasks with priority lists
  - Upgrades: Speed, capacity, task specialization
- Manager (Future)
  - Boost aura around processing or cashier areas
- Security (Future)
  - Reduces shrinkage/theft during crowding

## 5) Resources and Production Chains

Raw -> Processed -> Shelf-ready:
- Wheat -> Dough -> Bread (Bakery shelf)
- Milk -> Bottled Milk (Cold shelf)
- Tomatoes -> Canned Tomatoes (Canned shelf)
- Corn -> Processed Corn (Produce or Packaged)
- Wheat + Milk -> Cookie Dough -> Cookies (Bakery display)

Other raw items:
- Eggs (direct sale or future recipes)
- Apples (direct sale)

Nodes:
- Harvest Nodes: Fields, trees, coop, cow stall
- Machine Inputs: Pallet/slot in Processing Area
- Machine Outputs: Pallet/slot in Processing Area
- Stock Shelves: Supermarket Zone displays

Each node has:
- Capacity: Max units stored
- Refill/Regrowth: Timer-based for fields and livestock
- Worker/Player interaction: Pickup/Drop with capacity rules

## 6) Shops and Storage Plan

Storage Layers:
- Raw Storage (near sources)
  - Barn (Livestock West): eggs, milk
  - Sheds (Agriculture East): wheat, corn, tomatoes, apples
- Intermediate Storage (Processing Area)
  - Input pallets per machine
  - Output pallets per machine
- Retail Backroom (Supermarket Zone)
  - Cold storage: milk, future cold goods
  - Dry storage: bread, cookies, canned items
- Front-of-House Shelves
  - Bakery: bread shelf, cookie display
  - Central display: apples, tomatoes, corn
  - Canned goods: canned tomatoes, etc.

Shelf Layout (top zone):
- Left: Checkout counters with queue markers
- Center: Produce tables (apples, tomatoes, corn)
- Right: Bakery (bread shelves, cookies, future drinks)
- Back wall (behind center): canned shelves, cold case

## 7) Upgrades

Categories:
- Workers
  - Hire count (unlock more assistants)
  - Speed +5% per level
  - Capacity +2 per level
  - Specialization unlocks (stocker/harvester/cashier)
- Machines
  - Tomato canner: speed, queue size, power efficiency
  - Corn processor: speed, yield boost
  - Dough mixer + oven: speed, parallel slots
  - Milk bottler: speed, auto-labeler (reduces worker trips)
  - Cookie line: speed, batch size
- Devices/Facilities
  - Conveyors (future)
  - Pallet jacks/forklifts (player or worker use)
  - Cold storage capacity
- Player
  - Movement speed
  - Carry capacity
  - Interaction radius
- Storefront
  - Additional checkout counters
  - Shelf capacity and restock speed
  - Customer queue lanes and signage (improves patience)

UI References (from screenshots):
- Upgrades panel has sections for Workers, Machines, Devices
- Each entry shows cost, level, effect
- Settings
  - Volume slider (shown in screenshot)
  - Haptics toggle
  - Graphics toggle (Low/Med/High)
- HUD
  - Currency top-right
  - Current objective (optional)
  - Save indicator (bottom-left)

## 12) Save/Load

- Persistent data:
  - Currency, upgrades, hired workers
  - Shelf stock, storage inventories
  - Machine queues
  - Field/livestock growth timers
- Auto-save:
  - On upgrade purchase
  - Every 30 seconds
  - On app suspend/quit

## 13) Technical Implementation Notes (Unity)

- Scene Organization
  - One main scene “MallScene”
  - Sub-prefabs for each zone and machine type
- Systems
  - InventorySystem: Tracks item counts per node
  - TaskSystem: Assigns jobs to workers
  - CustomerSystem: Spawner + AI state machine
  - EconomySystem: Pricing, sales, and income
  - UpgradeSystem: Data-driven upgrades and effects
  - SaveSystem: JSON or binary, versioned schema
- Performance
  - Use object pooling for customers and items
  - Limit NavMesh updates; use grid waypoints where possible
- Mobile
  - Use CAMetalLayer/Metal where available (already supported in native layer)
  - Optimize draw calls via atlases and static batching

## 14) Expansion Roadmap

- New departments: Dairy aisle, beverages, snacks
- New recipes: Cakes (wheat + eggs + milk), cheese (milk -> cheese), sauces (tomato + spices)
- Events: Rush hours, holidays, supplier discounts
- Vehicles: Delivery van for special orders
- Cosmetics: Store themes and decorations affecting rating

## 15) Acceptance Criteria

- A new player can:
  - Harvest raw items, process them, and sell at least three SKUs
  - Hire one worker and see tangible impact
  - Purchase at least one machine upgrade
- The store shows:
  - Active customer flow with checkout behavior
  - Restocking behavior by workers
  - Profits increasing with upgrades and stock variety

## 16) Glossary

- SKU: Distinct sellable item (Bread, Cookies, Bottled Milk, Apples, Tomatoes, Corn, Canned Tomatoes, Eggs)
- Node: Any interactable source/sink of items
- Shelf: Front-of-house display node
- Pallet: Temporary storage node near machines

---

This file is the single source of truth. Update values here first; implementation should follow this spec.
