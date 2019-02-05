﻿using System;
using UnityEngine;
using UnityEngine.UI;

namespace Fps
{
    public class ConnectScreenController : MonoBehaviour
    {
        public Button ConnectButton;

        public void Awake()
        {
            ConnectButton.onClick.AddListener(ConnectClicked);
        }

        public void ConnectClicked()
        {
            if (ConnectionStateReporter.AreConnected)
            {
                ConnectionStateReporter.TrySpawn();
            }
            else
            {
                ConnectionStateReporter.TryConnect();
            }
        }
    }
}