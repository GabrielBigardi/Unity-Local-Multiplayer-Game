﻿using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu]
[Serializable]
public class SpriteAnimation : ScriptableObject
{
    public string animationName = "animation";

    public string Name;

    public int FPS;

    public List<SpriteAnimationFrame> Frames;

    public SpriteAnimationType SpriteAnimationType;
}