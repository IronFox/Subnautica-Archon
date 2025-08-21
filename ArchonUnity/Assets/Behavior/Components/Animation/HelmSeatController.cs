using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Behavior.Adapters;
using Behavior.Util;
using Behavior.Util.Log;
using UnityEngine;

public class HelmSeatController : MonoBehaviour
{
	public enum State 
	{
		Parked,
		Controlling,
		AnimateToControlPosition,
		AnimateToParkPosition
	}
	private State state = State.Controlling;
	private float animationProgress = 0f;
	public float animationSeconds = 1f;
	public Transform seatParkPosition;
	public Transform seatControlPosition;
	public Location animationStart;

	public bool IsParked => state == State.Parked;

	// Start is called before the first frame update
	void Start()
    {
        
    }
	private void AnimateToPosition(Location target, State targetState)
	{
		animationProgress += Time.deltaTime / animationSeconds;
		if (animationProgress >= 1f)
		{
			animationProgress = 1f;
			state = targetState;
			target.ApplyTo(transform);
			return;
		}
		Location.Lerp(animationStart, target, animationProgress).ApplyTo(transform);
	}

    // Update is called once per frame
    void Update()
    {
		switch (state)
		{
			case State.AnimateToParkPosition:
				{
					var target = Location.FromGlobal(seatParkPosition);
					AnimateToPosition(target, State.Parked);
				}
				break;
			case State.AnimateToControlPosition:
				{
					var target = Location.FromGlobal(seatControlPosition);
					AnimateToPosition(target, State.Controlling);
				}
				break;
		}
    }

	public void MoveToParkPosition()
	{
		state = State.Parked;
		transform.position = seatParkPosition.position;
		transform.rotation = seatParkPosition.rotation;
	}

	public void MoveToControlPosition()
	{
		state = State.Controlling;
		transform.position = seatControlPosition.position;
		transform.rotation = seatControlPosition.rotation;
	}

	public void AnimateToParkPosition()
	{
		if (state == State.Parked)
			return;
		state = State.AnimateToParkPosition;
		animationStart = Location.FromGlobal(transform);
		animationProgress = 0f;
	}

	public void AnimateToControlPosition()
	{
		if (state == State.Controlling)
			return;
		state = State.AnimateToControlPosition;
		animationStart = Location.FromGlobal(transform);
		animationProgress = 0f;
	}

	internal Parentage Reparent(Transform trailSpaceCameraContainer)
	{
		var seatOrigin = Parentage.FromLocal(transform);
		transform.SetParent(trailSpaceCameraContainer, false);
		transform.localPosition = Vector3.zero;
		transform.localRotation = Quaternion.identity;
		return seatOrigin;
	}

	public Parentage SeatPlayer(PlayerReference player)
	{
		using (var log = new LogContext(nameof(SeatPlayer)))
		{
			log.Write(
				$"Helm seat seating player {player.Root.NiceName()} at height = {player.HeadToSeatedHeightDifference}");
			var p = Parentage.FromLocal(player.Root.transform);
			player.Root.transform.SetParent(transform, false);
			player.Root.transform.localPosition = M.V3(0, -player.HeadToSeatedHeightDifference, 0);
			player.Root.transform.localRotation = Quaternion.identity;
			return p;
		}
	}
}
