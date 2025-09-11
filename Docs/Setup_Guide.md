# College Chronicles - Setup Guide

## Overview
Complete implementation guide for the College Chronicles prototype, including scene setup, prefab creation, and testing procedures.

## Phase 1: Scene Setup

### 1. Create Required Scenes
Create the following scenes in the `Assets/Scenes/` folder:
- **MainMenu.unity** - Entry point with New Game button
- **DormRoom.unity** - Introduction dialogue scene
- **CampusQuad.unity** - Minigame and conclusion scene

### 2. Scene Configuration

#### MainMenu Scene
- Add UI Canvas
- Create "New Game" button that calls `NavigationManager.GoToDormRoom()`
- Add basic title graphics

#### DormRoom Scene
- Add GameManager prefab with all core managers
- Set up DialogueUI_Controller prefab
- Add LocationTrigger for Campus Quad navigation
- Configure SmartphoneUI_Controller

#### CampusQuad Scene
- Copy GameManager from DormRoom
- Add RhythmDanceMinigame prefab
- Set up final dialogue completion

## Phase 2: Prefab Creation

### 1. GameManager Prefab
Create prefab with the following components:
```
GameManager (GameObject)
├── DialogueManager
├── PlayerStatsManager  
├── NavigationManager
├── TelemetryManager
└── ServiceLocator
```

### 2. DialogueUI Prefab Structure
```
DialogueUI (Canvas)
├── DialoguePanel
│   ├── CharacterPortrait (Image)
│   ├── SpeakerName (TextMeshPro)
│   ├── DialogueText (TextMeshPro)
│   ├── ChoicesContainer (Vertical Layout Group)
│   └── ContinueButton (Button)
├── StatsDisplay (TextMeshPro)
└── FeedbackContainer (for +1 Alpha/Beta notifications)
```

### 3. DialogueChoiceButton Prefab
```
ChoiceButton (Button)
├── Background (Image)
└── ChoiceText (TextMeshPro)
```

### 4. SmartphoneUI Prefab
```
SmartphoneUI (Canvas)
├── PhoneNotification
│   └── PhoneIcon (Button + Image)
└── MessagingPanel (full screen)
    ├── MessageContainer (Scroll Rect)
    └── CloseButton (Button)
```

### 5. RhythmMinigame Prefab  
```
MinigameUI (Canvas)
├── MinigamePanel
│   ├── Instructions (TextMeshPro)
│   ├── ArrowIndicators (4 Images: ↑↓←→)
│   ├── AccuracyText (TextMeshPro)
│   ├── SequenceText (TextMeshPro)
│   └── SkipButton (Button)
└── ResultPanel
    ├── ResultText (TextMeshPro)
    └── ContinueButton (Button)
```

### 6. LocationTrigger Prefabs
Create clickable areas for:
- **DormToCampus** - Triggers navigation to Campus Quad
- **CampusToDorm** - Triggers navigation back to Dorm Room

## Phase 3: ScriptableObject Data Setup

### 1. Character Data Assets
Create in `Assets/CollegeChronicles/Data/Characters/`:

#### MC_Character.asset
- **Name**: "Alex Chen"
- **ID**: "MC" 
- **Expressions**: Neutral sprite

#### Riley_Character.asset
- **Name**: "Riley Martinez"
- **ID**: "Riley"
- **Expressions**: Neutral, Happy sprites

#### Jordan_Character.asset
- **Name**: "Jordan Kim" 
- **ID**: "Jordan"
- **Expressions**: Neutral, Happy, Angry sprites

### 2. Choice Data Assets
Create choice assets in `Assets/CollegeChronicles/Data/Dialogue/`:

#### Introduction Choices
- **Choice_Alpha_Professor**: "Lead the way. I like intense professors" (+1 Alpha)
- **Choice_Beta_Nervous**: "That sounds great! I'm nervous about meeting everyone" (+1 Beta)
- **Choice_Neutral_Coffee**: "Sure, coffee sounds good" (No impact)

#### Project Discussion Choices
- **Choice_Alpha_Initiative**: "Great initiative, Jordan" (+1 Alpha)
- **Choice_Beta_Collaborative**: "I'd love to hear everyone else's ideas too" (+1 Beta)
- **Choice_Neutral_Listen**: "Cool, what did you have in mind?" (No impact)

#### Campus Quad Choices
- **Choice_Alpha_Risk**: "Come on Jordan, let's show them what we can do!" (+1 Alpha)
- **Choice_Beta_Support**: "Want to try this together, Jordan?" (+1 Beta)

### 3. Dialogue Node Assets
Create dialogue nodes following the script in `Docs/Dialogue_Script.md`

### 4. Dialogue Database Asset
Create main database in `Assets/CollegeChronicles/Data/`:
- Link all characters, choices, and dialogue nodes
- Set introduction and campus quad starting nodes

## Phase 4: Audio Setup

### Required Audio Files
Place in `Assets/CollegeChronicles/Audio/`:
- **background_music.wav** - Looping background track
- **ui_click.wav** - Button click sound
- **phone_buzz.wav** - Phone notification
- **correct_input.wav** - Minigame success sound
- **incorrect_input.wav** - Minigame failure sound
- **minigame_music.wav** - Rhythm game background

## Phase 5: Art Assets

### Character Sprites
Place in `Assets/CollegeChronicles/Art/Characters/`:
- **MC_Neutral.png**
- **Riley_Neutral.png**, **Riley_Happy.png**
- **Jordan_Neutral.png**, **Jordan_Happy.png**, **Jordan_Angry.png**

### UI Elements
Place in `Assets/CollegeChronicles/Art/UI/`:
- **dialogue_box.png** - 9-slice dialogue background
- **choice_button_normal.png** - Choice button normal state
- **choice_button_hover.png** - Choice button hover state
- **phone_icon.png** - Smartphone notification icon
- **arrow_up.png**, **arrow_down.png**, **arrow_left.png**, **arrow_right.png**

### Background Images
Place in `Assets/CollegeChronicles/Art/Backgrounds/`:
- **dorm_room.png** - Dorm room background
- **campus_quad.png** - Campus quad background

## Phase 6: Testing Setup

### 1. QA Testing Checklist

#### Smoke Test (3 Minutes)
- [ ] Application launches without errors
- [ ] Main Menu "New Game" button works
- [ ] First dialogue scene loads correctly
- [ ] Choice buttons are clickable and responsive

#### Full Playtest (15 Minutes)
- [ ] Start new game from main menu
- [ ] Read introduction dialogue completely
- [ ] Make Alpha choice and verify score increases
- [ ] Confirm Alpha score displays as "Alpha: 1 | Beta: 0"
- [ ] See free-roam mode activate in Dorm Room
- [ ] Click phone notification and read messages
- [ ] Navigate to Campus Quad using location trigger
- [ ] Start minigame interaction with Jordan
- [ ] Complete rhythm minigame with >60% accuracy
- [ ] Verify success dialogue path opens
- [ ] Reach prototype end screen

#### Edge Cases
- [ ] Test minigame skip functionality
- [ ] Verify Beta choices update scores correctly
- [ ] Test all scene transitions work smoothly
- [ ] Confirm personality feedback animations work

### 2. Performance Validation
- [ ] Scene transitions complete within 2 seconds
- [ ] No memory leaks during extended play
- [ ] Consistent 60fps throughout all scenes
- [ ] All audio triggers work correctly

### 3. Telemetry Validation
- [ ] session_start event fires on game launch
- [ ] choice_made events fire for all dialogue choices
- [ ] Telemetry data saved to persistent storage
- [ ] No telemetry errors in console

## Phase 7: Build Configuration

### Build Settings
- **Target Platform**: PC (Windows x64)
- **Scenes in Build**:
  1. MainMenu
  2. DormRoom  
  3. CampusQuad
- **Company Name**: College Chronicles Team
- **Product Name**: College Chronicles Prototype
- **Version**: 0.1.0

### Quality Settings
- **Texture Quality**: High
- **Anti-Aliasing**: 2x Multi Sampling
- **VSync**: Every V Blank
- **Target Frame Rate**: 60 FPS

## Troubleshooting

### Common Issues
1. **ServiceLocator Not Found**: Ensure GameManager is in scene before other managers
2. **Dialogue Not Starting**: Check DialogueDatabase assignment in DialogueManager
3. **Choices Not Working**: Verify ChoiceData assets are properly linked
4. **Telemetry Errors**: Ensure Newtonsoft.Json package is imported
5. **Animation Issues**: Confirm DOTween is properly imported and initialized

### Debug Commands
Each manager has context menu debug functions:
- DialogueManager: "Start Introduction", "Start Campus Quad"
- NavigationManager: "Go To Dorm Room", "Go To Campus Quad"
- RhythmMinigame: "Test Start Minigame", "Test Skip Minigame"

## Success Criteria

### Technical Goals
- [x] All core systems implemented and integrated
- [x] Controller/UI separation maintained throughout
- [x] Telemetry system captures required events
- [x] Scene management handles async loading properly

### Gameplay Goals  
- [ ] 5-minute introduction story functions correctly
- [ ] Branching dialogue choices affect personality scores
- [ ] Phone notification system guides player objectives
- [ ] Minigame integrates smoothly with narrative flow
- [ ] Free-roam navigation between scenes works

### Quality Goals
- [ ] No critical bugs in final build
- [ ] All UI interactions feel responsive and polished
- [ ] Character voices remain consistent throughout
- [ ] Visual style meets aesthetic standards

The prototype is ready for comprehensive testing and iteration based on QA feedback.