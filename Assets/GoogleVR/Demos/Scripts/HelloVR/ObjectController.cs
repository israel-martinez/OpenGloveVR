// Copyright 2014 Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System.Threading;
using DLLTest;
using OpenGloveAPI_Unity;
using System.Collections.Generic;

namespace GoogleVR.HelloVR {
  using UnityEngine;
  using UnityEngine.EventSystems;

  [RequireComponent(typeof(Collider))]
  public class ObjectController : MonoBehaviour {
    private Vector3 startingPosition;
    private Renderer myRenderer;
    public Material inactiveMaterial;
    public Material gazedAtMaterial;
	
	private int min = 0;
	private int max = 100;

	public static OpenGlove openGloveDevice = new OpenGlove("Left Hand", "OpenGloveIZQ", "OpenGloveVRTestConfig", "ws://127.0.0.1:7070");
	List<int> actuatorRegions = new List<int> { 0, 1, 2, 3, 4 };
	List<int> actuatorPositivePins = new List<int> { 11, 10, 9, 3, 6 };
	List<int> actuatorNegativePins = new List<int> { 12, 15, 16, 2, 8 };

    void Start() {
      startingPosition = transform.localPosition;
      myRenderer = GetComponent<Renderer>();
      SetGazedAt(false);
	  if(!openGloveDevice.IsConnectedToWebSocketServer) manageInitialOpenGloveConnection();
	  Debug.Log("Random: " + MyUtilities.GenerateRandom(min, max));
    }

	private void manageInitialOpenGloveConnection() {
	  new Thread(() => {
		  Thread.CurrentThread.IsBackground = true;
		  openGloveDevice.ConnectToWebSocketServer();
		  openGloveDevice.Communication.WebSocket.Send("Hello From OpenGloveVR");
		  openGloveDevice.Communication.WebSocket.Send("openGloveDevice.IsConnectedToWebSocketServer" + openGloveDevice.IsConnectedToWebSocketServer);
		  Thread.Sleep(30);
		  openGloveDevice.Communication.WebSocket.Send("2;OpenGloveIZQ;;;");
		  openGloveDevice.Communication.WebSocket.Send("9;OpenGloveIZQ;;;");
		  while (!openGloveDevice.IsConnectedToBluetoohDevice) {
		    //openGloveDevice.ConnectToBluetoothDevice();
			openGloveDevice.Communication.WebSocket.Send("5;OpenGloveIZQ;;;");
			Thread.Sleep(1000);
		  }

		  openGloveDevice.Communication.WebSocket.Send("12;OpenGloveIZQ;0,1,2,3,4;11,10,9,3,6;12,15,16,2,8");
		  openGloveDevice.Communication.WebSocket.Send("0;OpenGloveIZQ;;;");
		  //openGloveDevice.AddActuators(actuatorRegions, actuatorPositivePins, actuatorNegativePins);
		}).Start();
	}

	private void activateActuators(int miliseconds) {
		new Thread (() => {
	      Thread.CurrentThread.IsBackground = true;
		  openGloveDevice.Communication.WebSocket.Send("Try Activate actuator for " + miliseconds + " miliseconds");
		  //openGloveDevice.TurnOnActuators();
		  openGloveDevice.Communication.WebSocket.Send("17;OpenGloveIZQ;;;");
		  Thread.Sleep(miliseconds);
		  openGloveDevice.Communication.WebSocket.Send("18;OpenGloveIZQ;;;");
		  //openGloveDevice.TurnOffActuators();
		}).Start();
	}

    public void SetGazedAt(bool gazedAt) {
      if (inactiveMaterial != null && gazedAtMaterial != null) {
        myRenderer.material = gazedAt ? gazedAtMaterial : inactiveMaterial;
        return;
      }
    }

    public void Reset() {
      int sibIdx = transform.GetSiblingIndex();
      int numSibs = transform.parent.childCount;
      for (int i=0; i<numSibs; i++) {
        GameObject sib = transform.parent.GetChild(i).gameObject;
        sib.transform.localPosition = startingPosition;
        sib.SetActive(i == sibIdx);
      }
    }

    public void Recenter() {
#if !UNITY_EDITOR
      GvrCardboardHelpers.Recenter();
#else
      if (GvrEditorEmulator.Instance != null) {
        GvrEditorEmulator.Instance.Recenter();
      }
#endif  // !UNITY_EDITOR
    }

    public void TeleportRandomly(BaseEventData eventData) {
      // Pick a random sibling, move them somewhere random, activate them,
      // deactivate ourself.
      int sibIdx = transform.GetSiblingIndex();
      int numSibs = transform.parent.childCount;
      sibIdx = (sibIdx + Random.Range(1, numSibs)) % numSibs;
      GameObject randomSib = transform.parent.GetChild(sibIdx).gameObject;

      // Move to random new location ±100º horzontal.
      Vector3 direction = Quaternion.Euler(
          0,
          Random.Range(-90, 90),
          0) * Vector3.forward;
      // New location between 1.5m and 3.5m.
      float distance = 2 * Random.value + 1.5f;
      Vector3 newPos = direction * distance;
      // Limit vertical position to be fully in the room.
      newPos.y = Mathf.Clamp(newPos.y, -1.2f, 4f);
      randomSib.transform.localPosition = newPos;
	  openGloveDevice.Communication.WebSocket.Send("Treasure change of position!!!");
	  activateActuators(500);
      randomSib.SetActive(true);
      gameObject.SetActive(false);
      SetGazedAt(false);
    }
  }
}
