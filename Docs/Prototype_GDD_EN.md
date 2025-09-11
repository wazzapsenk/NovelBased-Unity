# Prototype Spec: "College Chronicles" - Vertical Slice

**Document Version:** 1.0
**Date:** September 11, 2025
**Author:** NAVA (Novelty-Aware Video-game Architect)

---

## 1. Purpose and Goals

The primary purpose of this prototype is to prove that the core gameplay loop of **"College Chronicles"** is technically feasible and experientially engaging. This is not a demo, but a **risk-mitigation tool.**

### Success Goals:
- **[Goal 1]** Validate that the branching dialogue and personality (Alpha/Beta) system works and provides immediate feedback to the player.
- **[Goal 2]** Test that the smartphone UI functions as a basic objective guide for the player.
- **[Goal 3]** Demonstrate that a basic minigame can be integrated without disrupting the narrative flow.
- **[Goal 4]** Prove that the presentation of 3D rendered character assets and their basic expression changes within Unity is aesthetically acceptable.

---

## 2. Scope

This prototype covers only the minimum content and features required to validate the goals listed above.

### In Scope:
-   **Narrative:** 1 introductory story scene (~5 minutes of gameplay) and 1 branching point following this scene.
-   **Systems:**
    -   Dialogue Choice System (Choice -> Consequence).
    -   Basic Alpha/Beta Personality Score Tracking.
    -   Smartphone UI (incoming message and objective display only).
-   **Locations:** 2 free-roam locations (e.g., "Dorm Room," "Campus Quad").
-   **Characters:** Main Character (MC) + 2 NPCs.
-   **Minigame:** 1 simple Rhythm/QTE (Quick Time Event) game.
-   **Telemetry:** Basic integration of the `choice_made` event.

### Out of Scope:
-   Save/Load system.
-   Complex inventory or economy.
-   Settings menu (Audio, Graphics, etc.).
-   Character customization.
-   End-of-episode summary screen.
-   Full animations (static renders and simple transitions will be used).

---

## 3. Feature Breakdown

### 3.1. Dialogue & Choice System
-   **Requirement:** The player must be able to click on one of 2-3 presented choices.
-   **Technical Detail:** Each choice should be defined as a `ScriptableObject` (`ChoiceData`). This object must contain:
    -   `string choiceText`: The text to be displayed on the button.
    -   `PersonalityImpact`: A struct like `(alphaPointChange, betaPointChange)`.
    -   `NextDialogueNode`: The next dialogue node to go to when this choice is selected.
-   **Feedback:** Upon making a choice, a visual effect like "+1 Alpha" should be displayed on screen (can be a simple scale/fade with DOTween).

### 3.2. Personality System (Alpha/Beta)
-   **Requirement:** There must be a manager (`PlayerStatsManager`) that holds and updates the player's Alpha and Beta scores.
-   **Visualization:** A simple text field must be present on the UI to display these scores (e.g., "Alpha: 5 | Beta: 2").

### 3.3. Smartphone UI
-   **Requirement:** At a specific story point, a phone icon should appear and vibrate. When clicked, it should open a full-screen messaging interface.
-   **Content:** For the prototype, only 1 predefined message chain will be shown ("Come to the campus quad.").

### 3.4. Free Roam
-   **Requirement:** The player must be able to transition between locations by clicking on clickable areas (`LocationTrigger`) on a 2D map.
-   **Technical Detail:** Each `LocationTrigger`, when clicked, should load the respective location's scene using `SceneManager.LoadSceneAsync`.

### 3.5. Minigame: "Rhythm Dance"
-   **Requirement:** In a specific scene, a QTE must be initiated, asking the player to press the correct arrow keys at the right time.
-   **Logic:** A 5-step sequence. >60% accuracy triggers the "Success" outcome. Otherwise, it triggers the "Failure" outcome. The story must continue based on either outcome.
-   **Skip:** A "Skip" button must be present on the screen. Clicking it will count the minigame as a "Failure" and continue the story.

---

## 4. Asset List

-   **[ ] Character Renders:**
    -   MC (Expression: Neutral)
    -   NPC_A (Expressions: Neutral, Happy)
    -   NPC_B (Expressions: Neutral, Angry)
-   **[ ] Location Backgrounds:**
    -   Dorm Room (Day)
    -   Campus Quad (Day)
-   **[ ] UI Elements:**
    -   Dialogue Box (PNG)
    -   Choice Button (Normal, Hover states)
    -   Smartphone UI (Messaging screen mock-up)
    -   Arrow Key Icons (Up, Down, Left, Right)
-   **[ ] Audio:**
    -   1 background music track (Loop)
    -   UI click sound (SFX)
    -   Minigame success/failure sounds (SFX)

---

## 5. Technical Requirements & Contracts

-   **State Separation:** All systems must be separated into `Controller` (logic) and `UI` (view) layers (e.g., `DialogueManager` and `DialogueUI_Controller`).
-   **Dependency Injection:** Zenject should be used for managing references between systems.
-   **Prefab Contract (For Developer Agent):**
    -   `DialogueChoice_Button.prefab`:
        -   **Public API:** `public void Initialize(ChoiceData choiceData)`
        -   **Events:** `public event Action<ChoiceData> OnChoiceSelected;`
-   **Telemetry (For Data Scientist Agent):**
    -   `session_start(session_id, device, version)`
    -   `choice_made(scene_id, choice_id, alpha_impact, beta_impact)`

---

## 6. Testing & Success Criteria (For QA Agent)

### Smoke Test (3 Minutes)
1.  Does the application launch?
2.  Can a "New Game" be started from the main menu?
3.  Does the first dialogue scene load?
4.  Are the choice buttons clickable?

### Playtest Scenario (15 Minutes)
1.  Start a new game.
2.  Read the introductory dialogues.
3.  Select the "Alpha" choice when presented.
4.  Verify that the "Alpha" score on the UI increases by 1.
5.  Confirm that after the dialogue, the free-roam mode starts in the "Dorm Room".
6.  Click the smartphone notification and read the message.
7.  Navigate to the "Campus Quad" from the map.
8.  Start the scene with NPC_B and play the Rhythm minigame.
9.  Successfully complete the minigame and see that the corresponding dialogue path opens.
10. Verify that the prototype end-screen is displayed.

### Evaluation of Success Criteria
-   **[Yes/No]** Does making a choice and seeing the result feel satisfying?
-   **[Yes/No]** Does the smartphone clearly communicate what to do next?
-   **[Yes/No]** Does the minigame feel like a fun break, or an obstacle to the flow?
-   **[Yes/No]** Is the visual style and presentation of the characters in line with the quality target?