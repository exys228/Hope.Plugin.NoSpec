using System.Collections.Generic;
using HOPEless.Bancho;
using HOPEless.Bancho.Objects;
using HOPEless.Extensions;
using HOPEless.osu;
using osu_HOPE.Plugin;
using osu.Shared;
using exys228.HopeCommands;

/* 
 * The main reason i created NoSpec is to show how HopeCommands works.
 * Don't search for hidden meaning in this plugin.
 */

namespace Hope.Plugin.NoSpec
{
	public class NoSpecPlugin : CommandBase, IHopePlugin
	{
		private bool Activated = true;

		private string BeatmapHash = "ffffffffffffffffffffffffffffffff";
		private string ActionText = "something"; // In user search menu: "Playing something"
		private int BeatmapID = 0;
		private Mods BeatmapMods = Mods.None;
		private GameMode PlayMode = GameMode.Standard;

		private const string PluginName = "NoSpec";
		private const string ChannelName = "#nospec";

		private int? UserID = null;

		private Queue<BanchoPacket> ServerReplies = new Queue<BanchoPacket>();

		public NoSpecPlugin() : base(PluginName, ChannelName)
		{

		}

		public PluginMetadata GetMetadata()
		{
			return new PluginMetadata
			{
				Name = PluginName,
				Author = "exys",
				ShortDescription = "Fucks up activity data so no one can see what beatmap are you playing.",
				Version = "1.0"
			};
		}

		public void Load()
		{
			AddCommand("nsp", "turn NoSpec on/off", delegate (string[] args)
			{
				Activated ^= true;
				SendPlayerMessage(PluginName, "Turned " + (Activated ? "on" : "off") + "!", ChannelName, UserID.Value);
			});

			/*
			AddCommand("h", "set fake beatmap hash", delegate(string[] args)
			{
				if (args.Length < 1)
				{
					SendPlayerMessage(PluginName, "", ChannelName, UserID.Value); // todo: what even is that??
					return;
				}
					
			});
			*/
		}

		public override void OnBanchoRequest(ref List<BanchoPacket> plist)
		{
			base.OnBanchoRequest(ref plist);

			for (int i = 0; i < plist.Count; i++)
			{
				switch (plist[i].Type)
				{
					case PacketType.ClientSpectateData:
					{
						if (Activated) plist.RemoveAt(i);
						break;
					}

					case PacketType.ClientUserStatus:
					{
						if (!Activated) break;

						BanchoUserStatus status = new BanchoUserStatus();

						status.Populate(plist[i].Data);

						if (status.Action == BanchoAction.Playing || status.Action == BanchoAction.Multiplaying)
						{
							status.BeatmapChecksum = BeatmapHash;
							status.ActionText = ActionText;
							status.CurrentMods = BeatmapMods;
							status.BeatmapId = BeatmapID;
							status.PlayMode = PlayMode;

							plist[i].Data = status.Serialize();
						}

						break;
					}
				}
			}
		}

		public override void OnBanchoResponse(ref List<BanchoPacket> plist)
		{
			base.OnBanchoResponse(ref plist);

			for (int i = 0; i < plist.Count; i++)
			{
				switch (plist[i].Type)
				{
					case PacketType.ServerLoginReply:
					{
						BanchoInt userid = new BanchoInt(plist[i].Data);

						if (userid.Value >= 0)
						{
							UserID = userid.Value;
							SendPlayerMessage(PluginName, "Hey there, you're using NoSpec plugin! Enter !help to get list of available commands. Good luck :з", ChannelName, UserID.Value);
						} 
						else UserID = null;

						break;
					}
				}
			}

			if (ServerReplies.Count > 0)
				plist.Add(ServerReplies.Dequeue());
		}

		private void SendPlayerMessage(string sender, string message, string channel, int senderid)
		{
			ServerReplies.Enqueue
			(
				new BanchoPacket(PacketType.ServerChatMessage,
				new BanchoChatMessage(sender, message, channel, senderid))
			);
		}
	}
}