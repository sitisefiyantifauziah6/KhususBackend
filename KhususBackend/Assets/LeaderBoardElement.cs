using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LeaderBoardElement : MonoBehaviour
{
    [SerializeField] TMP_Text LB_username;
    [SerializeField] TMP_Text LB_stat1;
    [SerializeField] TMP_Text LB_stat2;
    [SerializeField] TMP_Text LB_stat3;

    public void New_LBElement(string u, int s1, int s2, int s3)
    {
        LB_username.text = u;
        LB_stat1.text = s1.ToString();
        LB_stat2.text = s2.ToString();
        LB_stat3.text = s3.ToString();
    }
}