using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 成立時出目データに基づいてリールの表示パターンを構築・表示するクラス
/// </summary>
public class ReelPatternBuilder : MonoBehaviour
{
	/// <summary>
	/// リール間の表示間隔（x方向）
	/// </summary>
	[Header("成立時出目データ")]
	[SerializeField] protected float ComaDX;

	private GameObject[] BonusInBG;
	private GameObject[] BonusInStopInfo;
	private GameObject[][] BonusInComaImg;
	private GameObject[][] BonusInComaID;
	private GameObject[] BonusInCutLine;

	private SlotMaker2022.LocalDataSet.ReelArray[][] ra;
	private SlotEffectMaker2023.Action.HistoryManager hm;
	private ReelChipHolder comaData;
	private float defaultLineHeight;

	/// <summary>
	/// 初期化処理。リール表示オブジェクトを生成・配置し、必要な参照を保持します。
	/// </summary>
	private void Awake()
	{
		// 成立時出目データ初期化
		const int reelNum = SlotMaker2022.LocalDataSet.REEL_MAX;
		const int showComaNum = SlotMaker2022.LocalDataSet.SHOW_MAX;

		BonusInBG = new GameObject[reelNum];
		BonusInStopInfo = new GameObject[reelNum];
		BonusInCutLine = new GameObject[reelNum];
		BonusInComaImg = new GameObject[reelNum][];
		BonusInComaID = new GameObject[reelNum][];
		ra = SlotMaker2022.MainROMDataManagerSingleton.GetInstance().ReelArray;

		for (int i = 0; i < reelNum; ++i)
		{
			BonusInComaImg[i] = new GameObject[showComaNum];
			BonusInComaID[i] = new GameObject[showComaNum];
		}

		// 先頭リールのプレハブを取得
		BonusInBG[0] = transform.Find("BG").gameObject;
		BonusInStopInfo[0] = transform.Find("Order").gameObject;
		BonusInCutLine[0] = transform.Find("Line").gameObject;
		BonusInComaImg[0][0] = transform.Find("ComaImg").gameObject;
		BonusInComaID[0][0] = transform.Find("ComaID").gameObject;

		defaultLineHeight = BonusInCutLine[0].transform.localPosition.y;

		// 各リールと各コマを複製して配置
		for (int i = 0; i < reelNum; ++i)
		{
			if (i > 0)
			{
				BonusInBG[i] = Instantiate(BonusInBG[0], transform);
				BonusInBG[i].transform.localPosition += new Vector3(ComaDX * i, 0, 0);

				BonusInStopInfo[i] = Instantiate(BonusInStopInfo[0], transform);
				BonusInStopInfo[i].transform.localPosition += new Vector3(ComaDX * i, 0, 0);

				BonusInCutLine[i] = Instantiate(BonusInCutLine[0], transform);
				BonusInCutLine[i].transform.localPosition += new Vector3(ComaDX * i, 0, 0);
			}

			for (int j = i == 0 ? 1 : 0; j < showComaNum; ++j)
			{
				BonusInComaImg[i][j] = Instantiate(BonusInComaImg[0][0], transform);
				BonusInComaImg[i][j].transform.localPosition += new Vector3(
					ComaDX * i,
					BonusInComaImg[0][0].GetComponent<RectTransform>().sizeDelta.y * j,
					0);

				BonusInComaID[i][j] = Instantiate(BonusInComaID[0][0], transform);
				BonusInComaID[i][j].transform.localPosition += new Vector3(
					ComaDX * i,
					BonusInComaImg[0][0].GetComponent<RectTransform>().sizeDelta.y * j,
					0);
			}
		}

		hm = SlotEffectMaker2023.Singleton.SlotDataSingleton.GetInstance().historyManager;
		comaData = ReelChipHolder.GetInstance();

		Reset();
	}

	/// <summary>
	/// パターン履歴データと説明文を元にリール表示を更新します
	/// </summary>
	/// <param name="nowPtn">現在の成立パターンデータ</param>
	/// <param name="pInfo">表示用の説明テキスト</param>
	public void SetData(SlotEffectMaker2023.Action.PatternHistoryElem nowPtn, string pInfo)
	{
		transform.Find("Info").GetComponent<TextMeshProUGUI>().text = pInfo;

		if (nowPtn == null)
		{
			Reset();
			return;
		}

		string[] orderPtn = { "", "1st", "2nd", "3rd" };
		transform.Find("BetCount").GetComponent<TextMeshProUGUI>().text = nowPtn.BetNum.ToString() + "BET";

		for (int i = 0; i < BonusInBG.Length; ++i)
		{
			BonusInCutLine[i].GetComponent<Image>().enabled = false;
			BonusInStopInfo[i].GetComponent<TextMeshProUGUI>().text = orderPtn[nowPtn.StopOrder[i]] + " - [" + nowPtn.SlipCount[i].ToString() + "]";

			for (int j = 0; j < BonusInComaImg[i].Length; ++j)
			{
				int showComa = (j + nowPtn.ReelPos[i]) % SlotMaker2022.LocalDataSet.COMA_MAX;

				BonusInComaImg[i][j].GetComponent<Image>().enabled = true;
				BonusInComaID[i][j].GetComponent<Image>().enabled = true;

				// データは逆順に格納されていることに注意する。
				BonusInComaImg[i][j].GetComponent<Image>().sprite =
					comaData.ReelChipDataMini.Extract(ra[i][SlotMaker2022.LocalDataSet.COMA_MAX - showComa - 1].Coma);

				BonusInComaID[i][j].transform.Find("Text").GetComponent<TextMeshProUGUI>().text = (showComa + 1).ToString();

				if (showComa == 0)
				{
					BonusInCutLine[i].GetComponent<Image>().enabled = true;
					Vector3 pos = BonusInCutLine[i].transform.localPosition;
					pos.y = defaultLineHeight + BonusInComaImg[0][0].GetComponent<RectTransform>().sizeDelta.y * j;
					BonusInCutLine[i].transform.localPosition = pos;
				}
			}
		}
	}

	/// <summary>
	/// 全てのリール表示情報を初期状態（非表示）にリセットします
	/// </summary>
	public void Reset()
	{
		transform.Find("BetCount").GetComponent<TextMeshProUGUI>().text = string.Empty;

		for (int i = 0; i < BonusInBG.Length; ++i)
		{
			BonusInStopInfo[i].GetComponent<TextMeshProUGUI>().text = string.Empty;
			BonusInCutLine[i].GetComponent<Image>().enabled = false;

			for (int j = 0; j < BonusInComaImg[i].Length; ++j)
			{
				BonusInComaImg[i][j].GetComponent<Image>().enabled = false;
				BonusInComaID[i][j].GetComponent<Image>().enabled = false;
				BonusInComaID[i][j].transform.Find("Text").GetComponent<TextMeshProUGUI>().text = string.Empty;
			}
		}
	}
}
