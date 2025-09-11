using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using EditorAttributes;


public class TweenSequence : MonoBehaviour
{
    [Min(-1)] public int sequenceOnEnable = -1;
    [ShowField("@sequenceOnEnable != -1")]
    public bool enableLoop;
    [ShowField("enableLoop", true)]
    [Min(-1)] public int enableLoopCount;
    public List<TweenDataList> sequences;
    List<DG.Tweening.Sequence> sequenceList;

    private void OnValidate()
    {
        if (sequences == null)
            return;

        foreach (var sequence in sequences)
        {
            if (sequence == null || sequence.TweenDatas == null)
                continue;

            foreach (var tweenData in sequence.TweenDatas)
            {
                tweenData.AutoSelectTargetVisual();
            }
        }
    }

    void Awake()
    {

        // sequenceList = DOTween.Sequence();
        BuildSequence();
    }

    private void OnEnable()
    {
        if (sequenceOnEnable > -1)
        {
            if (enableLoop)
            {
                StartSequenceAsLoop(sequenceOnEnable, enableLoopCount);
            }
            else
            {
                StartSequence(sequenceOnEnable);
            }
        }
    }

    private void BuildSequence(int index = 0)
    {
        DOTween.Init();
        sequenceList = new List<DG.Tweening.Sequence>();
        foreach (var sequence in sequences)
        {
            sequenceList.Add(sequence.BuildSequence());
        }
    }

    public float StartSequence(int index = 0)
    {
        if (sequenceList == null)
        {
            BuildSequence();
        }
        var duration = sequenceList[index].Duration();
        sequenceList[index].Restart();
        return duration;
    }

    public void StartSequenceIfInactive(int index = 0)
    {
        if (!IsSequenceActive(index))
        {
            StartSequence(index);
        }
    }

    public void StartSequenceAsLoop(int index, int count)
    {
        if (sequenceList == null)
        {
            BuildSequence();
        }
        sequenceList[index].SetLoops(count).Restart();
    }

    [Button]
    public void TestSequence(int index, int loop = 0)
    {
        BuildSequence();
        if (loop != 0)
        {
            sequenceList[index].SetLoops(loop).Restart();
        }
        else
        {
            sequenceList[index].Restart();

        }
    }

    public bool IsSequenceActive(int index)
    {
        return sequenceList[index].IsPlaying();
    }

    public float GetSequenceDuration(int index = 0)
    {
        return sequenceList[index].Duration();
    }

    public void PauseAllSequences()
    {
        foreach (var sequence in sequenceList ?? Enumerable.Empty<Sequence>())
        {
            sequence.Pause();
        }
    }

}
