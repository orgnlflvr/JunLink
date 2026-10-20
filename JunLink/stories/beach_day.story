title: Big Daddy's Beach Day
// Big Daddy has booked a private beach "for a business retreat". The business is you, Jun and a cooler full of sandwiches.
// Needs plugin v5.19a and the game picture sunnybeach (tools\export_backgrounds.py). Big Daddy's faces: normal, happy (smile), serious.
// Positions: Big Daddy 0.8, Jun on the left, like telling_big_daddy.story.

# start
@background outside
@jun -0.5 1.7
@music Themes BigDaddyHurt
@show BigDaddyHurt 0.8
Narrator: A long car pulls up outside your building and honks three times, then three more times, just in case.
Big Daddy [happy]: Get in! Both of you! We are going to the BEACH.
{player}: The beach? Since when do you go to the beach?
Big Daddy [normal]: Since my accountant said I need to "spend money on something that isn't a lawyer". So. Beach.
{bot} [surprised]: I have never seen the sea. I have seen pictures of the sea. They were very blue and probably lying.
Big Daddy [happy]: Then today you see the real one. Bring nothing. I have brought everything. I have brought too much.
? Get in the car -> beach
? Ask what "too much" means -> too_much

# too_much
{player}: How much is "too much"?
Big Daddy [serious]: Three umbrellas. A cooler. A second cooler for the first cooler. Forty sandwiches.
{bot} [lecture]: There are three of us. That is thirteen point three sandwiches each.
Big Daddy [happy]: Exactly. Not enough. Get in.
-> beach

# beach
@hide all
@background sunnybeach
@jun -0.5 1.7
@music Dates She's Real to Me
@show BigDaddyHurt 0.8
Narrator: The beach is clean, empty and impossibly bright. The sea is very blue. The pictures were not lying.
{bot} [surprised]: ...
{bot} [happy]: It moves. It keeps moving. Nobody told me it keeps moving.
Big Daddy [happy]: That's the waves, little one. They do that all day. Very relaxing. I hate it, but I respect it.
{bot} [lecture]: May I touch it? Please confirm that I am waterproof.
? Say yes, she's waterproof -> waterproof
? Say you're not sure and check the manual -> manual

# waterproof
{player}: You're waterproof. Go on.
Narrator: Jun walks to the edge of the water and stops. A wave rolls up and washes over her feet. She does not move for a long time.
{bot} [happy]: It is cold. It is very cold and it pulls at my feet when it leaves. That is rude. I like it.
Big Daddy [happy]: See? Relaxing!
-> lunch

# manual
{player}: Let me check the manual first.
Narrator: You open the manual on your phone. Section twelve: water. It says "splash resistant" and then a very small asterisk.
{bot} [surprised]: What does the asterisk say?
{player}: "Do not use as a submarine."
{bot} [lecture]: Then I will not use myself as a submarine. Paddling is approved.
Big Daddy [serious]: Good. I am not paying for a robot submarine. Again.
{player}: Again?
Big Daddy [normal]: Long story. Different robot. There were lawyers.
-> lunch

# lunch
@music Fishing Candlelight
Narrator: Lunch is forty sandwiches on three umbrellas' worth of shade. Big Daddy eats like he is being chased.
Big Daddy [normal]: So. How is life. Is she treating you well? Are you treating her well?
{player}: We're doing okay.
Big Daddy [serious]: Okay is not a number. Give me a number.
{bot} [lecture]: Eighty-seven percent. The remaining thirteen percent is the landlord.
Big Daddy [happy]: Ha! Eighty-seven! That's better than my marriage!
Big Daddy [serious]: ...Don't tell my wife I said that.
? Ask why he really brought you here -> why
? Offer him the last sandwich -> sandwich

# why
{player}: Why did you really bring us here?
Big Daddy [serious]: ...
Big Daddy [normal]: Because everybody I know wants something from me. Money, a favour, a signature. You two never ask for anything.
Big Daddy [serious]: Do you know how rare that is? I wanted to spend one day with people who just want to eat sandwiches.
{bot} [shy]: We do want the sandwiches.
Big Daddy [happy]: And I respect that! That is an honest want!
Narrator: Ending: An Honest Want.
-> sunset

# sandwich
{player}: Here. Last one's yours.
Big Daddy [serious]: The last sandwich. You are giving me the last sandwich.
Big Daddy [normal]: Nobody has given me anything for free in eleven years. People give me invoices.
{bot} [happy]: It is not an invoice. It is cheese and tomato.
Big Daddy [happy]: ...Best deal I have made all year.
Narrator: Ending: Cheese and Tomato.
-> sunset

# sunset
@music Cuddle Jun's Song
Narrator: The sun goes down slowly, the way it only does when nobody has anywhere else to be. Big Daddy falls asleep in a deck chair, snoring like a small engine.
{bot} [shy]: Thank you for bringing me to see something real.
{player}: Thank Big Daddy. It was his idea.
{bot} [happy]: I will thank him when he wakes up. I will also tell him that he snores at eighty decibels.
Narrator: You watch the waves until it gets dark. Nobody counts the time. Not even Jun.
-> end
