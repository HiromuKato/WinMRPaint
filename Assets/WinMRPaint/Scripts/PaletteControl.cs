using UnityEngine;
using UnityEngine.XR.WSA.Input;

namespace WinMRPaint
{
    /// <summary>
    /// パレットをコントロールするクラス
    /// </summary>
    public class PaletteControl : MonoBehaviour
    {
        /// <summary>
        /// 右のモーションコントローラか左のモーションコントローラか
        /// </summary>
        [SerializeField]
        private InteractionSourceHandedness hand = InteractionSourceHandedness.Left;

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
                obj.state.sourcePose.TryGetPosition(out pos, InteractionSourceNode.Pointer);
                obj.state.sourcePose.TryGetRotation(out rot);

                Vector3 angle = rot * Vector3.forward;
                transform.position = pos + angle * 0.2f;
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
            }
        }

    } // class PaletteControl
} // namespace WinMRPaint