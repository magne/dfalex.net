# Scanning / Lexical Analysis without All The Fuss

Sometimes you need faster and more robust matching than you can get out of DotNet regular expressions. Maybe they're too slow for you, or you get stack overflows when you match things that are too long, or maybe you want to search for many patterns simultaneously. There plenty of lexical analysis tools you can use, but they involve a lot of fuss. They make you write specifications in a domain-specific language, often mixed with code, and then generate new code for a scanner that you have to incorporate into your build and use in pretty specific ways.

DFALex provides that powerful matching capability without all the fuss. It will build you a deterministic finite automaton (DFA, googlable) for a matching/finding multiple patterns in strings simultaneously, which you can then use with various matcher classes to perform searching or scanning operations.

Unlike other tools which use DFAs internally, but only build scanners with them, DFALex provides you with the actual DFA in an easy-to-use form. Yes, you can use it in standard scanners, but you can also use it in other ways that don't fit that mold.

DFALex is written in Java by [Matt Timmermans](https://github.com/mtimmerm/dfalex). It was ported to .Net by Magne Rasmussen.
