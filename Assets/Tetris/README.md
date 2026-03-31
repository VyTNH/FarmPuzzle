# Tetris Warehouse Game

This is a Tetris game integrated with warehouse management for mobile, where blocks represent crops from inventory.

## Features
- Standard Tetris gameplay with Tetromino shapes
- Blocks colored and textured based on external data (crops from inventory)
- Line clear calculates crop quantities and checks mission completion
- Mobile touch controls at the bottom

## Setup Instructions

1. Create a new Scene called "TetrisScene" in Assets/Scenes/

2. In the scene, create a GameObject named "Board" with:
   - Board.cs script
   - Child Tilemap (Grid -> Tilemap)

3. Create a GameObject named "ActivePiece" with Piece.cs script

4. Create a GameObject named "GhostPiece" with GhostPiece.cs script

5. Create a GameObject named "Inventory" with Inventory.cs script, and populate crops list with your crop data

6. Assign references in Board.cs:
   - Tetrominoes array with TetrominoData (each with sprite from crops)
   - Inventory reference

7. For mobile controls, add TouchControls.cs to a GameObject, assign board and activePiece

8. Create Tiles for each crop color/sprite

9. Build and run on mobile device

## Scripts Overview
- Board.cs: Manages the game board, spawning pieces, line clearing
- Piece.cs: Handles active Tetromino movement and rotation
- GhostPiece.cs: Shows preview of where piece will land
- TouchControls.cs: Handles touch input for mobile
- Inventory.cs: Manages crop items and mission checking
- TetrominoData.cs: Data for each Tetromino type
- Data.cs: Static data for shapes and wall kicks

## Mission System
After each line clear, the game calculates the number of each crop color cleared and adds to inventory. Check against mission requirements to complete tasks.