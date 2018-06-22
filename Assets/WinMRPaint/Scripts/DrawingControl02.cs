﻿using UnityEngine;
using UnityEngine.XR.WSA.Input;

namespace WinMRPaint
{
    /// <summary>
    /// DrawingControl02
    /// ペイント処理を行うクラス
    /// 機能：
    /// - 白いペンで描画ができる
    /// - ペンのサイズ変更ができる
    /// </summary>
    public class DrawingControl02 : MonoBehaviour
    {
        /// <summary>
        /// ラインオブジェクト
        /// </summary>
        public GameObject myLine;

        /// <summary>
        /// 右のモーションコントローラか左のモーションコントローラか
        /// </summary>
        [SerializeField]
        private InteractionSourceHandedness hand = InteractionSourceHandedness.Right;

        /// <summary>
        /// モーションコントローラのID
        /// </summary>
        private uint id;

        /// <summary>
        /// モーションコントローラの位置
        /// </summary>
        private Vector3 pos;

        /// <summary>
        /// モーションコントローラの回転
        /// </summary>
        private Quaternion rot;

        /// <summary>
        /// モード
        /// </summary>
        private enum actmode
        {
            none,
            draw
        }
        private actmode mode = actmode.draw;

        /// <summary>
        /// ペンのサイズ
        /// </summary>
        private float size = 0.01f;

        /// <summary>
        /// トリガー押下のたびに生成するラインオブジェクト
        /// </summary>
        private GameObject lines;

        /// <summary>
        /// 生成されたラインオブジェクトにつけられたLineRenderer
        /// </summary>
        private LineRenderer line;

        /// <summary>
        /// ラインの中に含まれるポイントの数
        /// </summary>
        private int pointCount = 1;

        /// <summary>
        /// 連続した接触の判定で利用するゲームオブジェクト
        /// </summary>
        private GameObject beforeTriggerd;

        // モーションコントローラのグリップから35度傾けるための値
        private static Quaternion r35 = Quaternion.Euler(35, 0, 0);

        /// <summary>
        /// ボタンを押下しているかどうか
        /// </summary>
        private bool buttonPressed = false;

        /// <summary>
        /// 初期化処理
        /// </summary>
        void Start()
        {
            // コールバックの設定
            InteractionManager.InteractionSourceDetected += InteractionManager_InteractionSourceDetected;
            InteractionManager.InteractionSourceUpdated += InteractionManager_InteractionSourceUpdated;
            InteractionManager.InteractionSourcePressed += InteractionManager_InteractionSourcePressed;
            InteractionManager.InteractionSourceReleased += InteractionManager_InteractionSourceReleased;
        }

        /// <summary>
        /// 入力ソースが検出されたときの処理
        /// </summary>
        /// <param name="obj">入力情報</param>
        private void InteractionManager_InteractionSourceDetected(InteractionSourceDetectedEventArgs obj)
        {
            // モーションコントローラーを認識してID取得
            if (obj.state.source.handedness == hand)
            {
                id = obj.state.source.id;
            }
        }

        /// <summary>
        /// ボタンが押下されたときの処理
        /// </summary>
        /// <param name="obj">入力情報</param>
        private void InteractionManager_InteractionSourcePressed(InteractionSourcePressedEventArgs obj)
        {
            if (obj.state.source.id == id)
            {
                buttonPressed = true;
                if (obj.state.selectPressedAmount > 0)
                {
                    Debug.Log("トリガーボタンが押下されました");

                    if (mode == actmode.none)
                    {
                        return;
                    }
                    else if (mode == actmode.draw)
                    {
                        // ラインを生成する
                        lines = Instantiate(myLine);
                        line = lines.GetComponent<LineRenderer>();
                        line.SetPosition(0, pos);

                        //ペンの太さ設定
                        line.startWidth = size;
                        line.endWidth = size;

                        pointCount = 1;
                    }
                }
            }
        }

        /// <summary>
        /// 入力ソースの情報が更新されたときの処理
        /// </summary>
        /// <param name="obj">入力情報</param>
        private void InteractionManager_InteractionSourceUpdated(InteractionSourceUpdatedEventArgs obj)
        {
            if (obj.state.source.id == id)
            {
                // 位置回転の取得
                obj.state.sourcePose.TryGetPosition(out pos, InteractionSourceNode.Pointer);
                obj.state.sourcePose.TryGetRotation(out rot);

                // モーションコントローラの少し先にポインタを描画する
                Vector3 angle = rot * r35 * Vector3.forward;
                transform.position = pos + angle * 0.1f;
                pos = transform.position;

                // スティックの上下操作によるペンの太さ変更
                if (Mathf.Abs(obj.state.thumbstickPosition.y) > 0.3f)
                {
                    if (obj.state.thumbstickPosition.y > 0)
                    {
                        size += Time.deltaTime / 30;
                    }
                    else if (obj.state.thumbstickPosition.y < 0)
                    {
                        size -= Time.deltaTime / 30;
                    }

                    if (size < 0.005f) size = 0.005f;
                    if (size > 1) size = 1f;
                    transform.localScale = new Vector3(size, size, size);
                }

                // ボタンが押されていない場合は以降の処理を行わない
                if (buttonPressed == false)
                {
                    return;
                }

                // トリガーボタンが押されているときの処理
                if (obj.state.selectPressedAmount > 0)
                {
                    if (mode == actmode.none)
                    {
                        return;
                    }
                    else if (mode == actmode.draw)
                    {
                        //線を追加
                        line.positionCount = pointCount + 1;
                        line.SetPosition(pointCount, pos);

                        //Colliderを追加
                        SphereCollider ccol = lines.AddComponent<SphereCollider>();
                        ccol.center = Vector3.Lerp(line.GetPosition(pointCount - 1), line.GetPosition(pointCount), 0.5f);
                        ccol.radius = size / 2;
                        ccol.isTrigger = true;
                        pointCount++;
                    }
                }
            }
        }

        /// <summary>
        /// ボタンが離されたときの処理
        /// </summary>
        /// <param name="obj">入力情報</param>
        private void InteractionManager_InteractionSourceReleased(InteractionSourceReleasedEventArgs obj)
        {
            if (obj.state.source.id == id)
            {
                Debug.Log("ボタンが離されました");
                buttonPressed = false;
            }
        }

    } // class DrawingControl
} // namespace WinMRPaint