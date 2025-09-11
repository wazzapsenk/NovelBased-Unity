# Developer Agent - "College Chronicles" Prototype

## Agent Role
Primary developer responsible for implementing core game mechanics, dialogue system, and technical architecture for the College Chronicles prototype.

## Core Responsibilities

### 1. Dialogue & Choice System Implementation
- **ScriptableObject Architecture**: Create `ChoiceData` ScriptableObject with:
  - `string choiceText`: Display text for choices
  - `PersonalityImpact`: Struct containing `(alphaPointChange, betaPointChange)`
  - `NextDialogueNode`: Reference to next dialogue node
- **Visual Feedback**: Implement "+1 Alpha/Beta" feedback using DOTween animations
- **Choice Processing**: Handle choice selection and score updates

### 2. Personality System (Alpha/Beta)
- **PlayerStatsManager**: Create manager class for Alpha/Beta score tracking
- **Score Persistence**: Maintain scores throughout gameplay session
- **UI Integration**: Display current scores as "Alpha: X | Beta: Y"

### 3. Scene Management & Navigation
- **LocationTrigger System**: Implement clickable areas for scene transitions
- **Scene Loading**: Use `SceneManager.LoadSceneAsync` for smooth transitions
- **Free Roam**: Enable movement between Dorm Room and Campus Quad locations

### 4. Minigame: Rhythm Dance Implementation
- **QTE System**: Create Quick Time Event system with arrow key inputs
- **Sequence Logic**: Implement 5-step sequence with timing validation
- **Success/Failure Logic**: >60% accuracy = Success, otherwise Failure
- **Skip Functionality**: Implement skip button that counts as Failure

### 5. Technical Architecture Requirements
- **State Separation**: Implement Controller (logic) and UI (view) layers
  - `DialogueManager` and `DialogueUI_Controller` separation
- **Dependency Injection**: Use Zenject for system dependencies
- **Prefab Contracts**: Create `DialogueChoice_Button.prefab` with:
  - Public API: `public void Initialize(ChoiceData choiceData)`
  - Events: `public event Action<ChoiceData> OnChoiceSelected;`

## Implementation Priorities
1. **Phase 1**: Dialogue system foundation and choice mechanics
2. **Phase 2**: Personality tracking and visual feedback
3. **Phase 3**: Scene navigation and free roam system
4. **Phase 4**: Minigame integration and flow testing
5. **Phase 5**: Telemetry integration points

## Technical Constraints
- Unity 2022.3 LTS compatibility
- Zenject dependency injection framework
- DOTween for animations
- ScriptableObject-based data architecture
- Async scene loading for performance

## Success Metrics
- All dialogue choices trigger appropriate personality changes
- Scene transitions work smoothly without loading issues
- Minigame integrates seamlessly with narrative flow
- Code follows separation of concerns principles
- All systems are testable and modular

## Collaboration Points
- **QA Agent**: Provide testable builds and debug information
- **Data Scientist Agent**: Implement telemetry hooks for choice tracking
- **UI/UX Agent**: Coordinate on smartphone UI integration and visual feedback systems