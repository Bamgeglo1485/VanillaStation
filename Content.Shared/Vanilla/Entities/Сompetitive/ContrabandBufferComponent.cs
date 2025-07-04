using Robust.Shared.Serialization;

namespace Content.Shared.Vanilla.Competitive;

[RegisterComponent]
public sealed partial class ContrabandBufferComponent : Component
{
    [DataField("analyzedItems")]
    public List<ContrabandAnalysisData> AnalyzedItems = new();
}

[Serializable, NetSerializable]
public sealed class ContrabandAnalysisData
{
    public string SourceName = "Неизвестный предмет";
    public List<char> Genome = new();
    public List<List<CodonFeedBack>> History = new();
    public CompetitiveDifficult Difficult = CompetitiveDifficult.medium;
    public int AttemptsCount = 0;
    public int CalculateResearchPointsAward()
    {
        return Difficult switch
        {
            CompetitiveDifficult.easy => 10_000,
            CompetitiveDifficult.medium => 30_000,
            CompetitiveDifficult.hard => 80_000,
            _ => 0
        };
    }

}

[Serializable, NetSerializable]
public sealed class CodonFeedBack
{
    public char Codon;
    public CodonHint Hint;
}


[Serializable, NetSerializable]
public enum CodonHint
{
    Correct,       // Зелёный — верная буква, верное место
    WrongPosition, // Жёлтый — буква есть, но место не то
    Incorrect      // Красный — буквы вообще нет в решении
}
