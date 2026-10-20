title: Amei - Why Did You Save Me
// A very sad "what if" for the clinic scene. It starts the moment you choose "Pull the plug". Everything here is new writing.
// It only uses the facts of the game scene: Amei is bound to her master, her master is on life support, and when the master is gone the next order in line takes over.
// Needs the v5.14k plugin. Amei slides towards the window with @slide (9 seconds) and @stop Melissa freezes her where she is when you shout WAIT.
// Positions are the game's own: Amei +0.75 and the patient -0.75 in the clinic, Amei +1.5 in the flashback. Tweak on the stage page (http://127.0.0.1:8765/stage).
// Clinic picture: the first time, press F7 in the game's own clinic scene (it saves BepInEx\jun_backgrounds\hos.png). After that this story uses it.
color: Amei = #6dc061

# start
@background hospital
@music Wish Things Were Easier
@show UnknownWoman -0.75
@show Melissa 0.75
@outfit Melissa Daily2
@face Melissa sad
Narrator: The machine beside the bed breathes in and breathes out. In and out. You have listened to it for so long that you have stopped hearing it.
Narrator: Your hand is on the plug. Amei stands next to you. She does not look at you. She looks at the bed.
Amei [sad]: You do not have to. I want you to know that. I will not hold it against you either way.
{player}: I know. That is why I am doing it.
Narrator: You pull. The room does not seem to change at all. The breathing just stops, and the quiet it leaves behind is enormous.
@hide UnknownWoman
@move Melissa 0
Amei [sad]: ...
Amei [sad]: It is very quiet.
Amei [sad]: Thank you. I mean it. Thank you.
Narrator: She bows to the bed, deeply, the way you bow at a grave. Then she walks to the window and puts her hand flat on the glass.
-> window

# window
Narrator: She turns away from you and walks, very slowly, towards the window.
@slide Melissa -1.6 9
Amei [sad]: She was thirteen the first time she told me my name was not Amei. It was just a word they wrote on a form. She said I should pick my own.
Amei [sad]: I never did. I did not know how.
Narrator: It is a high window. Below it, the car park is full of small grey cars, all very far away.
Amei [sad]: You were kind to me. I did not deserve that. When she is gone, the next order in line begins. It has been waiting inside me for years, very patiently.
Amei [sad]: It will not touch anyone but me. I made sure of that. I made sure of everything.
Narrator: She turns the handle. The cold air moves the curtains. Your whole body understands before your mind does.
-> wait

# wait
@stop Melissa
{player}: WAIT!
Narrator: You cross the room faster than you have ever moved. You do not grab her wrist. You grab the sleeve of her sweater, like a child.
Narrator: She does not pull away. She does not do anything at all.
Amei [sad]: ...
Amei [sad]: Why?
Amei [sad]: Why did you stop me? I did not ask you to.
Amei [sad]: I asked you for one thing, and you gave it to me. I asked you for nothing else.
Narrator: Her voice is not angry. That is the worst part. It is the voice of someone who has said "understood" ten thousand times, and has just been given an order she does not know how to follow.
? "Because I cannot lose you too." -> too
? "Because you never asked anybody for anything. Not once." -> never
? Say nothing. Hold on to her sleeve and cry. -> cry

# too
{player}: Because I just did it. I just pulled it. It is still in my hands, Amei. I cannot do it twice in one day.
Amei [sad]: So that is the reason. Not me. You.
{player}: Yes. No. Both. I do not know.
Amei [sad]: That is... honest. Nobody has ever been honest with me about what I was for.
Amei [sad]: Then I am a thing that makes you feel less alone, standing in a hospital window.
{player}: That is not what I-
Amei [sad]: It is alright. It is more than most things get to be.
-> after

# never
{player}: You asked me for one thing in all this time. One. And it was this. You never asked to eat what you liked, or to rest, or to be called by a name you chose.
Amei [sad]: Those were not mine to ask for.
{player}: They were. They are. They have been the whole time.
Amei [sad]: I do not know how to ask for things. I have only ever been told.
Narrator: She looks at her own hands as if they belonged to someone she once knew.
-> after

# cry
Narrator: You do not have anything wise to say. You say her name. Her real name, the one she wrote for herself in a clinic nobody visits.
Narrator: And somewhere in the middle of it she puts her hand over yours. Her hand is cold. It is trembling a very small amount, like a machine on standby.
Amei [sad]: ...You are crying for me.
Amei [sad]: I did not know anyone could. I did not ask.
-> after

# after
@background flashback
@music If Only
@show Melissa 1.5
@outfit Melissa Daily
@face Melissa sad
Narrator: She closes her eyes. You can tell what she is seeing, because for one second her face looks fourteen years old.
Narrator: A girl at a birthday table, cheeks full of cake, saying with her mouth full: "Amei, do not cry! It is MY birthday! You are not allowed to be sad on my birthday!"
Narrator: A bot standing very still, wiping her own face with her sleeve, because nobody had told her what her eyes were for.
Amei [sad]: She was so kind, once. I want you to know that. Before the helicopter. Before the pain. Before the sugar and the shouting.
Amei [sad]: She used to hold my hand in the cinema when the film was frightening. I told her I could not be frightened. She said that was fine, she would hold it anyway.
Amei [sad]: I miss her. I am not allowed to say that. I killed her.
-> grief

# grief
@background hospital
@music Wish Things Were Easier
@show Melissa 0.75
@outfit Melissa Daily2
@face Melissa sad
{player}: You did not. I did. And I would do it again, because it was the only thing that was left.
Amei [sad]: You are lying, to be kind.
{player}: Yes. A little. But we did it together. Neither of us gets to carry it alone.
Amei [sad]: ...
Amei [sad]: Then I am not alone with it.
Narrator: She leans against the wall beneath the window and slides down it, very slowly, until she is sitting on the cold floor with her knees pulled up.
Amei [sad]: I still hear the order. It is quiet, but it is there. It will be there tomorrow.
{player}: Then tomorrow I will sit next to you again.
? Ask her to let Jun in on the secret -> jun
? Stay where you are, on the floor, just the two of you -> floor

# jun
@music Take a deep breath
@jun -0.8 1.7
Narrator: You call Jun. You do not explain. You only say: "Please come. Do not bring Annalie."
Narrator: She is there in a minute and forty seconds. She stops in the doorway and looks at the two of you on the floor, and for once she does not say a single word.
{bot} [sad]: ...Understood.
Narrator: She sits down on the other side of Amei. Nobody asked her to.
{bot} [sad]: There is a way to put a new order above the old one. It must be written by you. It cannot be written by anyone else.
Amei [sad]: I do not know what I want it to say.
{bot} [sad]: Then we will wait until you do. I can wait for a very long time. I am very good at it.
-> end_jun

# end_jun
@music Jun's Song
Amei [sad]: I am sorry. I am sorry for all of this. For the trouble. For being...
{bot} [sad]: You are not trouble. You were never trouble.
Narrator: Amei puts her head on Jun's shoulder. Neither of them is able to cry properly. They are machines. They sit there together anyway, and it looks exactly like crying.
Narrator: The clinic's lights switch off one by one for the night. Nobody comes to tell you to leave.
Narrator: Ending: Still Here. She did not say thank you for stopping her. She said it a week later, once, very quietly, and then she asked for a cup of tea. It was the first thing she had ever asked for.
-> end

# floor
@hide Melissa
Narrator: Jun is not there. Annalie is not there. The clinic has only one light on, in a corridor.
Narrator: You sit on the floor. She sits on the floor. Somewhere a machine is not breathing. You both listen to the not-breathing for a very long time.
Amei [sad]: Do you want to know the worst part?
{player}: Yes.
Amei [sad]: I am not even sad. I am only tired. I thought that would be easier to bear.
Amei [sad]: And I am still here. And tomorrow I will still be here, and I do not know what to do with a tomorrow.
{player}: We find out. Slowly. Together.
Amei [sad]: ...That is a very hard thing to ask.
{player}: I know.
Narrator: She does not say yes. She does not say no. She stays. At dawn, a cleaner finds two people asleep against a wall, and does not wake them.
Narrator: Ending: Tomorrow. It was not a happy ending. It was only one where somebody was still there to see the morning.
-> end
