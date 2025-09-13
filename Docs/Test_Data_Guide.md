# College Chronicles - Test Data Implementation Guide

## Created Test Data Assets

### Character Data Files (Assets/CollegeChronicles/Data/Characters/)

#### 1. MC_AlexChen.asset
- **Character Name**: Alex Chen
- **ID**: MC
- **Description**: 19-year-old college freshman, main character
- **Voice**: Relatable, sometimes uncertain but eager to grow

#### 2. Riley_Martinez.asset
- **Character Name**: Riley Martinez  
- **ID**: Riley
- **Description**: Friendly roommate/study buddy
- **Voice**: Encouraging, uses "dude" and "totally"
- **Expressions**: Neutral, Happy

#### 3. Jordan_Kim.asset
- **Character Name**: Jordan Kim
- **ID**: Jordan  
- **Description**: Intense perfectionist classmate
- **Voice**: Direct, formal, achievement-focused
- **Expressions**: Neutral, Happy, Angry

#### 4. Narrator.asset
- **Character Name**: Narrator
- **ID**: Narrator
- **Description**: Omniscient narrator for scene descriptions

### Choice Data Files (Assets/CollegeChronicles/Data/Dialogue/)

#### Coffee Scene Choices:
1. **Choice_Alpha_Professor.asset** - "Lead the way. I like intense professors" (+1 Alpha)
2. **Choice_Beta_Nervous.asset** - "I'm nervous about meeting everyone" (+1 Beta)  
3. **Choice_Neutral_Coffee.asset** - "Sure, coffee sounds good" (No impact)

#### Project Discussion Choices:
4. **Choice_Alpha_Initiative.asset** - "Great initiative, Jordan" (+1 Alpha)
5. **Choice_Beta_Collaborative.asset** - "I'd love to hear everyone else's ideas" (+1 Beta)
6. **Choice_Neutral_Listen.asset** - "Cool, what did you have in mind?" (No impact)

#### Campus Quad Choices:
7. **Choice_Alpha_Risk.asset** - "Come on Jordan, let's show them!" (+1 Alpha)
8. **Choice_Beta_Support.asset** - "Want to try this together?" (+1 Beta)

### Dialogue Node Files (Assets/CollegeChronicles/Data/Dialogue/)

#### Introduction Scene Flow:
1. **Node_Opening.asset** - Narrator introduction (auto-continues)
2. **Node_MC_Reflection.asset** - Alex's internal monologue (auto-continues)
3. **Node_Riley_Enters.asset** - Riley offers coffee invitation (presents choices)
4. **Node_Riley_Response_Alpha.asset** - Riley's response to Alpha choice
5. **Node_Riley_Response_Beta.asset** - Riley's response to Beta choice  
6. **Node_Riley_Response_Neutral.asset** - Riley's response to Neutral choice
7. **Node_Walking_Campus.asset** - Walking to coffee conversation
8. **Node_Riley_Encouragement.asset** - Riley's supportive response
9. **Node_Jordan_Appears.asset** - Jordan introduces project (presents choices)
10. **Node_Phone_Trigger.asset** - Phone notification triggers (triggers phone UI)
11. **Node_Free_Roam_Start.asset** - Starts free-roam mode

#### Campus Quad Scene Flow:
12. **Node_Quad_Arrival.asset** - Arrival at campus quad
13. **Node_Jordan_Surprise.asset** - Jordan's reaction to dance event
14. **Node_Dance_Challenge.asset** - Dance challenge setup (presents choices)
15. **Node_Minigame_Start_Alpha.asset** - Pre-minigame Alpha path (triggers minigame)
16. **Node_Minigame_Start_Beta.asset** - Pre-minigame Beta path (triggers minigame)
17. **Node_Minigame_Success.asset** - Post-minigame success dialogue
18. **Node_Minigame_Failure.asset** - Post-minigame failure dialogue
19. **Node_Prototype_End.asset** - Final prototype completion message

### Main Database File

#### CollegeChronicles_DialogueDatabase.asset
- **Location**: Assets/CollegeChronicles/Data/
- **Contains**: All character, dialogue node, and choice references
- **Starting Nodes**: 
  - Introduction: Node_Opening
  - Campus Quad: Node_Quad_Arrival

## Implementation Steps in Unity

### Step 1: Script GUID Assignment
The test data uses placeholder GUIDs. In Unity:

1. **Import all scripts first** - Unity will assign real GUIDs to the script files
2. **Note the real GUIDs** for:
   - CharacterData script
   - DialogueNode script  
   - ChoiceData script
   - DialogueDatabase script

### Step 2: Update Asset Files
Replace placeholder GUIDs in all .asset files with real Unity-assigned GUIDs:

```yaml
# Replace this placeholder:
m_Script: {fileID: 11500000, guid: 7a7e1e1e1e1e1e1e1e1e1e1e1e1e1e1e, type: 3}

# With real GUID from Unity:
m_Script: {fileID: 11500000, guid: [REAL_GUID_FROM_UNITY], type: 3}
```

### Step 3: Link References in Database
1. Open DialogueDatabase in Unity Inspector
2. Drag all Character assets to the Characters array
3. Drag all Dialogue Node assets to the Dialogue Nodes array  
4. Drag all Choice assets to the Choices array
5. Set Introduction Node and Campus Quad Node references

### Step 4: Link Choices to Nodes
For each DialogueNode that should have choices:

#### Node_Riley_Enters choices:
- Choice_Alpha_Professor → Node_Riley_Response_Alpha
- Choice_Beta_Nervous → Node_Riley_Response_Beta
- Choice_Neutral_Coffee → Node_Riley_Response_Neutral

#### Node_Jordan_Appears choices:
- Choice_Alpha_Initiative → Node_Phone_Trigger
- Choice_Beta_Collaborative → Node_Phone_Trigger
- Choice_Neutral_Listen → Node_Phone_Trigger

#### Node_Dance_Challenge choices:
- Choice_Alpha_Risk → Node_Minigame_Start_Alpha
- Choice_Beta_Support → Node_Minigame_Start_Beta

### Step 5: Set Node Flow (nextNode references)
Auto-continuing nodes should reference their next node:
- Node_Opening → Node_MC_Reflection
- Node_MC_Reflection → Node_Riley_Enters
- Node_Riley_Response_[All] → Node_Walking_Campus
- Node_Walking_Campus → Node_Riley_Encouragement
- Node_Riley_Encouragement → Node_Jordan_Appears
- Node_Phone_Trigger → Node_Free_Roam_Start
- Node_Quad_Arrival → Node_Jordan_Surprise
- Node_Jordan_Surprise → Node_Dance_Challenge
- Node_Minigame_Start_[Both] → (handled by minigame system)
- Node_Minigame_Success → Node_Prototype_End
- Node_Minigame_Failure → Node_Prototype_End

### Step 6: Set Choice Next Nodes
Each ChoiceData asset needs its nextDialogueNode set:
- Choice_Alpha_Professor → Node_Riley_Response_Alpha
- Choice_Beta_Nervous → Node_Riley_Response_Beta
- Choice_Neutral_Coffee → Node_Riley_Response_Neutral
- Choice_Alpha_Initiative → Node_Phone_Trigger  
- Choice_Beta_Collaborative → Node_Phone_Trigger
- Choice_Neutral_Listen → Node_Phone_Trigger
- Choice_Alpha_Risk → Node_Minigame_Start_Alpha
- Choice_Beta_Support → Node_Minigame_Start_Beta

## Testing Checklist

### Data Validation
- [ ] All Character assets load without errors
- [ ] All Choice assets show correct personality impacts
- [ ] All Dialogue Nodes display text properly
- [ ] DialogueDatabase references all assets correctly

### Dialogue Flow Testing
- [ ] Introduction starts with Node_Opening
- [ ] Auto-continuing nodes progress automatically
- [ ] Choice nodes present correct options
- [ ] Personality scores update with choices
- [ ] Phone notification triggers at correct time
- [ ] Free-roam activates after phone sequence
- [ ] Campus Quad scene loads Node_Quad_Arrival
- [ ] Minigame triggers from both Alpha and Beta paths
- [ ] Success/Failure paths work correctly
- [ ] Prototype end message displays

### Personality System Testing
- [ ] Alpha choices increase Alpha score by 1
- [ ] Beta choices increase Beta score by 1  
- [ ] Neutral choices don't affect scores
- [ ] UI displays current scores correctly
- [ ] Visual feedback shows "+1 Alpha/Beta"

## Story Path Summary

### Maximum Alpha Path (Score: 3)
1. "Lead the way. I like intense professors" (+1 Alpha)
2. "Great initiative, Jordan" (+1 Alpha)  
3. "Come on Jordan, let's show them!" (+1 Alpha)
**Result**: Confident, leadership-oriented character development

### Maximum Beta Path (Score: 3)
1. "I'm nervous about meeting everyone" (+1 Beta)
2. "I'd love to hear everyone else's ideas" (+1 Beta)
3. "Want to try this together?" (+1 Beta)  
**Result**: Empathetic, collaborative character development

### Mixed Paths (Various Scores)
Players can choose any combination for different personality balance results.

## Asset Dependencies

The test data creates a complete, self-contained dialogue system that demonstrates:
- ✅ Branching dialogue with meaningful choices
- ✅ Personality system with immediate feedback
- ✅ Phone notification integration
- ✅ Free-roam state management
- ✅ Minigame story integration
- ✅ Success/failure dialogue paths
- ✅ Complete prototype experience (~5 minutes)

This test data is ready for immediate Unity implementation and testing.