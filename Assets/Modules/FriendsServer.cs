﻿using UnityEngine;
using System.Collections;
using System.Linq;
using uLobby;

public class FriendsServer : MonoBehaviour {
	private FriendsDB friendsDB;
	private LobbyGameDB lobbyGameDB;

	// Start
	void Start() {
		friendsDB = GetComponent<FriendsDB>();
		lobbyGameDB = GetComponent<LobbyGameDB>();
		
		// Make this class listen to lobby events
		Lobby.AddListener(this);
	}

	// --------------------------------------------------------------------------------
	// RPCs
	// --------------------------------------------------------------------------------
	
#region RPCs
	[RPC]
	IEnumerator AddFriend(string friendName, string groupName, LobbyMessageInfo info) {
		LobbyPlayer player = LobbyServer.GetLobbyPlayer(info);
		LogManager.General.Log(string.Format("Player '{0}' is adding player '{1}' to friend list group '{2}'!", player.account.name, friendName, groupName));
		
		// Find friends group
		var selectedGroup = player.friends.GetGroupByName(groupName);
		
		// Get account ID
		string friendAccountId = null;
		
		yield return StartCoroutine(lobbyGameDB.GetAccountIdByPlayerName(friendName, data => {
			friendAccountId = data;
		}));
		
		// Error getting account ID?
		if(friendAccountId == null) {
			Lobby.RPC("FriendAddPlayerDoesntExistError", info.sender, friendName);
			yield break;
		}
		
		// Trying to add yourself?
		if(friendAccountId == player.accountId) {
			Lobby.RPC("FriendAddCantAddYourselfError", info.sender, friendName);
			yield break;
		}
		
		// Already in friends list?
		if(!player.friends.CanAdd(friendAccountId)) {
			Lobby.RPC("FriendAddAlreadyExistsError", info.sender, friendName);
			yield break;
		}
		
		// Add player to the group
		selectedGroup.friends.Add(new Friend(friendAccountId));
		
		// Send new friends list
		player.OnFriendsListLoaded();
		
		// Save friends list in database
		yield return StartCoroutine(friendsDB.SetFriends(
			player.accountId,
			player.friends,
			null
		));
	}
	
	[RPC]
	IEnumerator RemoveFriend(string friendName, string groupName, LobbyMessageInfo info) {
		LobbyPlayer player = LobbyServer.GetLobbyPlayer(info);
		LogManager.General.Log(string.Format("Player '{0}' is removing player '{1}' from friend list group '{2}'!", player.account.name, friendName, groupName));
		
		// Find friends group
		var selectedGroup = player.friends.GetGroupByName(groupName);
		
		// Get account ID
		string friendAccountId = null;
		
		yield return StartCoroutine(lobbyGameDB.GetAccountIdByPlayerName(friendName, data => {
			friendAccountId = data;
		}));
		
		// Error getting account ID?
		if(friendAccountId == null) {
			Lobby.RPC("FriendRemovePlayerDoesntExistError", info.sender, friendName);
			yield break;
		}
		
		// Remove player from the group
		selectedGroup.friends.RemoveAll(friend => friend.accountId == friendAccountId);
		
		// Send new friends list
		player.OnFriendsListLoaded();
		
		// Save friends list in database
		yield return StartCoroutine(friendsDB.SetFriends(
			player.accountId,
			player.friends,
			null
		));
	}
	
	[RPC]
	IEnumerator SetFriendNote(string friendName, string groupName, string note, LobbyMessageInfo info) {
		LobbyPlayer player = LobbyServer.GetLobbyPlayer(info);
		LogManager.General.Log(string.Format("Player '{0}' sets friends list note for player '{1}' to '{2}'!", player.account.name, friendName, note));
		
		// Find friends group
		var selectedGroup = player.friends.GetGroupByName(groupName);
		
		// Get account ID
		string friendAccountId = null;
		
		yield return StartCoroutine(lobbyGameDB.GetAccountIdByPlayerName(friendName, data => {
			friendAccountId = data;
		}));
		
		// Error getting account ID?
		if(friendAccountId == null) {
			Lobby.RPC("SetFriendNotePlayerDoesntExistError", info.sender, friendName);
			yield break;
		}
		
		// Remove player from the group
		selectedGroup.friends.Find(friend => friend.accountId == friendAccountId).note = note;
		
		// Save friends list in database
		yield return StartCoroutine(friendsDB.SetFriends(
			player.accountId,
			player.friends,
			null
		));
	}
#endregion
}
