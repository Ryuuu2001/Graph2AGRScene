using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarkovChain
{
    // ×´Ì¬¸ÅÂÊ¾ØÕó
    public static double[][] probabilities = new double[][]
    {
        new double[] { 0.90, 0.05, 0.05, 0.05},
        new double[] { 0.93, 0.05, 0.00, 0.02},
        new double[] { 0.93, 0.00, 0.05, 0.02},
        new double[] { 0.90, 0.05, 0.05, 0.00}
    };

    // ×´Ì¬×ªÒÆº¯Êý
    public static int TransitionState(int currentState)
    {
        System.Random rand = new System.Random();
        double[] probs = probabilities[currentState];
        double randValue = rand.NextDouble();
        double cumulativeProb = 0;

        for (int i = 0; i < probs.Length; i++)
        {
            cumulativeProb += probs[i];
            if (randValue < cumulativeProb)
            {
                return i;
            }
        }

        return currentState;
    }
}
