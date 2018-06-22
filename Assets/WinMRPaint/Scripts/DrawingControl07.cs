using UnityEngine;
using UnityEngine.XR.WSA.Input;

namespace WinMRPaint
{
    /// <summary>
    /// DrawingControl07
    /// ペイント処理を行うクラス
    /// 機能：
    /// - 白いペンで描画ができる
    /// - ペンのサイズ変更ができる
    /// - ペンの色を変えることができる
    /// - 消しゴム機能追加
    /// - ペン時と消しゴム時でペン先の形状を変更する
    /// - サウンド再生
    /// - エフェクトの表示
    /// </summary>
    public class DrawingControl07 : MonoBehaviour
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
        /// パーティクル
        /// </summary>
        [SerializeField]
        private ParticleSystem particle;

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
            draw,
            effect,
            erase
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
        /// ペンのメッシュ
        /// </summary>
        private Mesh sphereMesh;

        /// <summary>
        /// 消しゴムのメッシュ
        /// </summary>
        private Mesh cubeMesh;

        /// <summary>
        /// パレット選択時の音
        /// </summary>
        private AudioSource selectSound;

        /// <summary>
        /// 消しゴムによる消去時の音
        /// </summary>
        private AudioSource eraseSound;

        /// <summary>
        /// 初期化処理
        /// </summary>
        void Start()
        {
            // ペン用のメッシュのキャッシュ
            sphereMesh = GetComponent<MeshFilter>().mesh;
            // 消しゴム用のメッシュのキャッシュ
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            cubeMesh = cube.GetComponent<MeshFilter>().mesh;
            cube.SetActive(false);

            // AudioSourceの設定
            AudioSource[] audioSources = GetComponents<AudioSource>();
            selectSound = audioSources[0];
            eraseSound = audioSources[1];

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

                        // ラインを消去するときに利用するタグ
                        lines.tag = "Drawn";

                        pointCount = 1;
                    }
                    else if (mode == actmode.effect)
                    {
                        // ボタンが押された場所にパーティクルを生成する
                        ParticleSystem par = Instantiate(particle);
                        par.transform.position = pos;
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

        /// <summary>
        /// オブジェクトが接触したときの処理(ラインを消去する)
        /// </summary>
        /// <param name="other">接触したオブジェクト情報</param>
        private void OnTriggerEnter(Collider other)
        {
            // 連続で同じものへ接触したときへの対処
            if (beforeTriggerd == other.gameObject)
            {
                return;
            }
            beforeTriggerd = other.gameObject;

            if (other.gameObject.tag == "PaletteObj")
            {
                mode = actmode.draw;

                // ペン先を球に変更
                GetComponent<MeshFilter>().mesh = sphereMesh;
                GetComponent<Renderer>().material = other.gameObject.GetComponent<Renderer>().material;
                myLine.GetComponent<Renderer>().material = other.gameObject.GetComponent<Renderer>().material;
                selectSound.Play();
            }
            else if (other.gameObject.tag == "Pen")
            {
                mode = actmode.draw;
                // ペン先を球に変更
                GetComponent<MeshFilter>().mesh = sphereMesh;
                selectSound.Play();
            }
            else if (other.gameObject.tag == "Effect")
            {
                mode = actmode.effect;
                // ペン先を球に変更
                GetComponent<MeshFilter>().mesh = sphereMesh;
                selectSound.Play();
            }
            else if (other.gameObject.tag == "Eraser")
            {
                mode = actmode.erase;
                // ペン先を四角に変更
                GetComponent<MeshFilter>().mesh = cubeMesh;
                selectSound.Play();
            }
            else if (other.gameObject.tag == "Drawn")
            {
                // ラインの消去
                if (mode == actmode.erase && buttonPressed == true)
                {
                    eraseSound.Play();
                    Destroy(other.gameObject);
                }
            }
        }

    } // class DrawingControl
} // namespace WinMRPaint