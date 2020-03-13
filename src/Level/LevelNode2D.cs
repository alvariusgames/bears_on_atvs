using Godot;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public abstract class LevelNode2D : Node2D, ILevel
{
    public Player Player;
    public Node2D NodeInst { get { return this;}}
    public String NodePath { get { return this.Filename;}}
    public abstract int LevelNum{ get;}
    public abstract String Title {get;}
    public int Zone { get { return LevelNode2D.GetZoneNumFromNodePath(this.NodePath);}}
    public abstract float UnitOffset { get;}
    public abstract String MusicPath{get;}
    public virtual String AmbiencePath{get;}

    private Boolean spaceRock1Collected = false;
    public Boolean SpaceRock1Collected { get { return this.spaceRock1Collected;} set{ this.spaceRock1Collected = value;}}
    private Boolean spaceRock2Collected = false;
    public Boolean SpaceRock2Collected { get { return this.spaceRock2Collected;} set{ this.spaceRock2Collected = value;}}
    private Boolean spaceRock3Collected = false;
    public Boolean SpaceRock3Collected { get { return this.spaceRock3Collected;} set{ this.spaceRock3Collected = value;}}
    private Boolean calledOnce = false;
    private float timeElapsedInLevel = 0f;
    private Boolean playerUnlocked = false;
    private const float TIME_TO_UNLOCK_PLAYER_SEC = 2.5f;
    public ParallaxBackground ParallaxBackground;

    public override void _Ready(){
        this.RemoveAllNonRefreshables();
        this.HydrateSpaceRocks();
        foreach(Node child in this.GetChildren()){
            if(child is Player){
                this.Player = (Player)child;
                this.Player.ATV.tempStopAllMovement(exceptGravity: true);
                this.playerUnlocked = false;
                this.Player.ActiveLevel = this;
                if(this.onLoadPlayerSafetyCheckpoint != null){
                    this.Player.SetMostRecentSafetyCheckPoint(this.onLoadPlayerSafetyCheckpoint);
                    this.Player.GoToMostRecentSafetyCheckPoint();}
                if(this.onLoadPlayerCalories != -1){
                    this.Player.TotalCalories = this.onLoadPlayerCalories;}}
            if(child is ParallaxBackground){
                this.ParallaxBackground = (ParallaxBackground)child;}}
    }

    public void RemoveAllNonRefreshables(){
        var nonRefreshables = DbHandler.GetNonRefreshablesFor(this.LevelNum);
        this._removeNonRefreshablesRecurs(this, nonRefreshables);
    }

    private void _removeNonRefreshablesRecurs(Node node, Dictionary<int, INonRefreshable> NonRefreshables){
        if(node is INonRefreshable){
            var nonRefreshableNode = (INonRefreshable)node;
            if(NonRefreshables.ContainsKey(nonRefreshableNode.UUID)){
                node.GetParent().RemoveChild(node);}}
        foreach(Node child in node.GetChildren()){
            this._removeNonRefreshablesRecurs(child, NonRefreshables);}
    }

    public void HydrateSpaceRocks(){
        var levelStatsRecord = DbHandler.GetLevelStatsRecordFor(this.LevelNum);
        if(this.SpaceRock1Collected == false){
            //False in this sense might mean uninitialized -- check from Db
            this.SpaceRock1Collected = DbHandler.GetLevelStatsRecordFor(this.LevelNum).SpaceRock1Collected;}
        if(this.SpaceRock2Collected == false){
            this.SpaceRock2Collected = DbHandler.GetLevelStatsRecordFor(this.LevelNum).SpaceRock2Collected;}
        if(this.SpaceRock3Collected == false){
            this.SpaceRock3Collected = DbHandler.GetLevelStatsRecordFor(this.LevelNum).SpaceRock3Collected;}
    }

    public void ChangeLevelModulate(Color PlayerModulate, float PlayerModTransThresh, 
                                    Color BackgroundModulate, float BgModTransThresh,
                                    Color EverythingElseModulate, float EvElseTransThresh){
        if(PlayerModTransThresh >= 1){
            PlayerModTransThresh = 1;}
        if(BgModTransThresh >= 1){
            BgModTransThresh = 1;}
        if(EvElseTransThresh >= 1){
            EvElseTransThresh = 1;}
        foreach(Node child in this.GetChildren()){
            if(child is Player){
                ((Player)child).Modulate = ((Player)child).Modulate * (1 - PlayerModTransThresh) + 
                                           PlayerModulate * PlayerModTransThresh;}
            else if(child is ParallaxBackground){
                foreach(ParallaxLayer parallaxLayer in child.GetChildren()){
                    parallaxLayer.Modulate = parallaxLayer.Modulate * (1 - BgModTransThresh) + 
                                             BackgroundModulate * BgModTransThresh;}}
            else if(child is Node2D){
                ((Node2D)child).Modulate = ((Node2D)child).Modulate * (1 - EvElseTransThresh) +
                                           EverythingElseModulate * EvElseTransThresh;}}
    }

    public LevelFrame TryGetLevelFrame(){
        var n = this.GetParent();
        while(!(n is LevelFrame)){
            n = n.GetParent();
            if(n == null){
                return null;}}
        return (LevelFrame)n;
    }

    private SafetyCheckPoint onLoadPlayerSafetyCheckpoint;
    public void OnLoadPlacePlayerAt(SafetyCheckPoint safetyCheckPoint){
        this.onLoadPlayerSafetyCheckpoint = safetyCheckPoint;}

    public int onLoadPlayerCalories = -1;
    public void OnLoadSetPlayerCaloriesTo(int Calories){
        this.onLoadPlayerCalories = Calories;
    }

    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";

    // Called when the node enters the scene tree for the first time.
    public override void _Process(float delta){
        this.timeElapsedInLevel += delta;
        if(!this.calledOnce){
            //SoundHandler.SetStreamVolume(0f); Why is this here?
            SoundHandler.PlayStream<MyAudioStreamPlayer>(this,
                new string[] {this.MusicPath},
                Loop: true);
            if(this.AmbiencePath != null){
                SoundHandler.PlayStream<MyAudioStreamPlayer>(this,
                    new string[] {this.AmbiencePath},
                    Loop: true);}
            this.calledOnce = true;}
        if(this.timeElapsedInLevel > TIME_TO_UNLOCK_PLAYER_SEC && !this.playerUnlocked){
            this.Player.ATV.resumeMovement();
            this.playerUnlocked = true;}    
        }
 
     public static int GetZoneNumFromNodePath(String NodePath){
        var match = (new Regex(@"(?<=z)[0-9]")).Match(NodePath); //Get number after the z
        if(match.Success){
            return Int32.Parse(match.Value);}
        else{
            return -1;}}

 }