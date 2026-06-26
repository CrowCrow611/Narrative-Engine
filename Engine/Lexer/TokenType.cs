namespace Engine.Lexer;

public enum TokenType {

    Identifier,
    StringLiteral,
    Integer,
    Float,

    KwScene,
    KwBeat,
    KwChoice,
    KwRequire,
    KwEffect,
    KwTag,
    KwSystems,
    KwGameConfig,
    KwGraph,
    KwNode,
    KwChapter,
    KwVoice,
    KwEmotion,

    LBrace,
    RBrace,
    LBracket,
    RBracket,
    LParen,
    RParen,
    Colon,
    Comma,
    Arrow,
    Dot,

    And,
    Or,
    Not,
    EqEq,
    NotEq,
    Lt,
    LtEq,
    Gt,
    GtEq,

    Newline,
    EndOfFile,
    Unknown,
}