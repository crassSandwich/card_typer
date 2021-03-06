﻿using System;
using System.Collections;
using System.Collections.Generic;
using CTShared.Networking;
using LiteNetLib.Utils;

namespace CTShared
{
public class MatchManager : Packet
{
    public event Action OnMatchStart, OnPreTypePhaseStart, OnTypePhaseStart, OnTypePhaseEnd, OnDrawPhaseStart, OnDrawPhaseEnd;
    public event Action<float> OnTypePhaseTick;

    public const float TypingTime = 10;
    public const float TypingCountdownTime = 3;

    public readonly Agent Player1, Player2;

    public float TypingTimer { get; private set; }
    public float CountdownTimer { get; private set; }
    public float TypingTimeLeftPercent => TypingTimer / TypingTime;

    public int CardsCastedThisTurn => Player1.CardsCastedThisTurn + Player2.CardsCastedThisTurn;

    internal readonly Random Rand = new Random();

    public bool InDrawPhase { get; private set; }
    public bool InPreTypingPhase { get; private set; }
    public bool InTypingPhase { get; private set; }
    
    bool started;
    bool player1Ready, player2Ready;

    public MatchManager (string player1DeckText, string player2DeckText)
    {
        try
        {
            Player1 = new Agent(this, player1DeckText);
        }
        catch (ArgumentException e)
        {
            throw new ArgumentException("error when parsing player 1's deck", e.InnerException);
        }

        try
        {
            Player2 = new Agent(this, player2DeckText);
        }
        catch (ArgumentException e)
        {
            throw new ArgumentException("error when parsing player 2's deck", e.InnerException);
        }

        Player1.OnPlaySet += p => agentReady(Player1);
        Player2.OnPlaySet += p => agentReady(Player2);
    }

    // for packet serialization
    internal MatchManager ()
    {
        Player1 = new Agent(this);
        Player2 = new Agent(this);
    }

    public void Start ()
    {
        if (started)
        {
            throw new InvalidOperationException("match is already started");
        }

        started = true;
        if (OnMatchStart != null) OnMatchStart();
        startDrawPhase();
    }

    public void Tick (float dt)
    {
        if (InPreTypingPhase)
        {
            CountdownTimer -= dt;
            if (CountdownTimer <= 0)
            {
                startTypePhase();
            }
        }

        if (InTypingPhase)
        {
            if (OnTypePhaseTick != null) OnTypePhaseTick(dt);

            TypingTimer -= dt;
            if (TypingTimer <= 0)
            {
                endTypePhase();
            }
        }
    }

    internal Agent GetEnemyOf (Agent agent)
    {
        if (agent == Player1)
        {
            return Player2;
        }
        else if (agent == Player2)
        {
            return Player1;
        }
        else
        {
            throw new Exception($"unexpected agent {agent}");            
        }
    }

    internal override void Deserialize (NetDataReader reader)
    {
        Card.CastLock = reader.GetBool();

        TypingTimer = reader.GetFloat();

        Player1.Deserialize(reader);
        Player2.Deserialize(reader);
    }

    internal override void Serialize (NetDataWriter writer)
    {
        writer.Put(Card.CastLock);

        writer.Put(TypingTimer);

        Player1.Serialize(writer);
        Player2.Serialize(writer);
    }

    void agentReady (Agent agent)
    {
        if (!InDrawPhase) return;

        if (agent == Player1)
        {
            player1Ready = true;
        }
        else if (agent == Player2)
        {
            player2Ready = true;
        }
        else
        {
            throw new Exception($"unexpected agent {agent}");
        }

        if (player1Ready && player2Ready)
        {
            endDrawPhase();
        }
    }

    void startDrawPhase ()
    {
        InDrawPhase = true;

        Player1.DrawNewHand();
        Player2.DrawNewHand();

        player1Ready = false;
        player2Ready = false;

        if (OnDrawPhaseStart != null) OnDrawPhaseStart();
    }

    void endDrawPhase ()
    {
        InDrawPhase = false;

        if (OnDrawPhaseEnd != null) OnDrawPhaseEnd();

        startPreTypePhase();
    }

    // countdown before true type phase
    void startPreTypePhase ()
    {
        InPreTypingPhase = true;
        CountdownTimer = TypingCountdownTime;

        if (OnPreTypePhaseStart != null) OnPreTypePhaseStart();
    }

    void startTypePhase ()
    {
        InPreTypingPhase = false;
        InTypingPhase = true;
        TypingTimer = TypingTime;

        if (OnTypePhaseStart != null) OnTypePhaseStart();
    }

    void endTypePhase ()
    {
        InTypingPhase = false;

        if (OnTypePhaseEnd != null) OnTypePhaseEnd();

        startDrawPhase();
    }
}
}
