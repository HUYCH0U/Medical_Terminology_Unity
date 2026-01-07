using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WordModel
{
    protected string StringValue;
    protected string IdValue;
    protected string Apologetic;
    protected Type WordType;

    public string quizQuestion;     
    public string[] quizAnswers;     
    public string correctApologetic; 
    public enum Type { Prefix, Root, Suffix, Terminology }

    public string stringvalue { get => StringValue; set => StringValue = value; }
    public string idvalue { get => IdValue; set => IdValue = value; }
    public string apologetic { get => Apologetic; set => Apologetic = value; }
    public Type type { get => WordType; set => WordType = value; }

    public virtual void Initialize(string stringValue, string idValue, string apologeticValue, Type wordType)
    {
        idvalue = idValue;
        stringvalue = stringValue;
        apologetic = apologeticValue;
        type = wordType;
    }

    public void SetQuizData(string question, string[] answers, string correctAns)
    {
        quizQuestion = question;
        quizAnswers = answers;
        correctApologetic = correctAns;
    }
}