namespace Nullframes.Intrigues
{
    public enum IResult {
        True = 0,
        False = 1,
        Null = 2
    }
    
    public enum NType
    {
        String = 0,
        Integer = 1,
        Bool = 2,
        Float = 3,
        Object = 4,
        Enum = 5,
        Actor = 6,
        Clan = 7,
        Family = 8,
    }
    
    public enum GenericNodeType
    {
        Scheme,
        Rule,
        Family,
        Clan,
        Policy,
        Actor,
        Culture,
        Variable,
    }
    
    /// <summary>
    /// Defines the types of policies based on their specific applicability or scope.
    /// </summary>
    public enum PolicyType {
        /// <summary>
        /// General policies that can be applied both as Family and Clan policies.
        /// </summary>
        Generic = 0,

        /// <summary>
        /// Policies specifically related to families. Only families can implement these policies.
        /// </summary>
        Family = 1,

        /// <summary>
        /// Policies specifically designed for clans. Only clans can implement these policies.
        /// </summary>
        Clan = 2,
    }

    public enum EnumType
    {
        Is = 0,
        IsNot = 1,
    }

    public enum ValidatorMode
    {
        Passive = 0,
        Active = 1,
        Break = 2,
    }

    public enum SchemeResult
    {
        None = 2,
        Success = 0,
        Failed = 1,
        Null = 2,
    }
    
    public enum RuleState
    {
        Failed = 0,
        Success = 1,
    }

    public enum MathOperation
    {
        Set = 0,
        Add = 1,
        Subtract = 2,
        Multiply = 3
    }

    public enum GenderFilter
    {
        None = 0,
        Male = 1,
        Female = 2,
        MaleFemale = 3,
        FemaleMale = 4
    }
    
    public enum AgeFilter
    {
        None = 0,
        Oldest = 1,
        Youngest = 2
    }
    
    public enum RelativeFilter
    {
        None = 0,
        Child = 1,
        Parent = 2,
        Sibling = 3,
        Spouse = 4,
        Uncle = 5,
        Aunt = 6,
        Grandparent = 7,
        Grandchild = 8,
        Nephew = 9,
        Niece = 10,
        BrotherInLaw = 11,
        SisterInLaw = 12
    }
    
    public enum KeyType {
        Down = 0,
        Up = 1,
        Hold = 2,
        Tap = 3
    }

    public enum DualType {
        Conspirator_Target,
        Target_Conspirator,
        GetActors
    }
}