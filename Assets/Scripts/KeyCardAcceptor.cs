﻿using System;
using UnityEngine;

public class KeyCardAcceptor : MonoBehaviour
{
    public bool Active { get; set; }

    public event Action<Transform> OnCollide = t => { };

    public void Collission(Transform obj)
    {
        OnCollide(obj);
    }
}