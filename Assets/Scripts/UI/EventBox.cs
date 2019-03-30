﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using crass;

public class EventBox : Singleton<EventBox>
{
    [SerializeField]
    TextMeshProUGUI contents;

    void Awake ()
    {
        SingletonSetInstance(this, true);
    }

    public void AddText (string text)
    {
        contents.text += text;
    }
}