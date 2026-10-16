# Truck Poker — rules stage

Implementation is in progress. The authoritative no-limit Hold’em core is implemented and validated; the playable table, private views, seated truck/dealer presentation, collection entry and final Windows delivery are still pending. No empty sport card has been added.

PokerTable owns deck/shuffle, blinds/button, betting streets, legal actions, full-raise/short-all-in reopening, runouts, settlement and tournament chip conservation. PokerHand selects the best five of five–seven cards, including the wheel. PokerPots resolves contribution bands, folded-hand ineligibility, ties, odd chips left of the button and uncalled returns. UI cannot inject a score. Solo-call action against a short all-in blind cannot invent a full-blind phantom call.

PokerPolicy receives only a copied PokerView for its own seat and an independent personal RNG. It has no table/deck/opponent-hand/future-shuffle reference. Difficulty changes samples, execution error and decisions; personality/bluffs do not read opponents’ private cards. Public snapshots cannot mutate the engine. Fold wins expose no hands; ordinary showdown tables required winners, while all-in showdowns table surviving hands. Future own-pane peek UI must clear its private display state on release, modal/focus/source transitions and actor handoffs; that presentation is not yet implemented.

Betting follows the current [Poker TDA rules45/49](https://www.pokertda.com/view-poker-tda-rules/) for minimum full raises and cumulative short all-in reopening, retrieved2026-10-16. Incomplete opening all-ins do not reopen a previous checker without a full bet faced. Interface actions are explicit; physical chip-declaration ambiguity rules do not apply. This is offline play with virtual chips, no real-money services.

Vendor audit: no suitable installed or accessible owned poker rules engine was found in the existing asset audit. LWS therefore owns these missing game rules; existing Unity Input System, Heat/TMP, DOTween and canonical truck presentation will provide the playable mode’s foundations. No vendor source modified.
