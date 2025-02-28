using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;

public class RakeTest : MonoBehaviour
{
    [TextArea]
    public string input;

    [ContextMenu("Execute RAKE")]
    public void Rake_Sort_Of_Works()
    {
        var rake = new SentenceSimilarityUnity.Rake();

        var result = rake.Run(input);

        foreach (var pair in result)
        {
            Debug.Log($"keword: {pair.Key}, score: {pair.Value}");
        }
    }

    [Test]
    public void Just_Looking_At_Some_Results()
    {
        var text =
            @"Iraq has launched the long-awaited offensive to expel Islamic State from its second largest city Mosul and Australian personnel and aircraft will certainly be involved in support operations.

            But Defence won't say just what or how.

            'Defence will not discuss specific details for operational security reasons,' a defence spokesman said.

            Defence Minister Marise Payne declined to comment on operational details, saying it would take time and she was awaiting updates.

            She also declined to elaborate on predictions of civilian casualties.

            'I don't think my conjecture on rates of casualties or otherwise would be helpful at this point,' she said.

            Australia has a substantial force in the Middle East, extensively involved in the fight against Islamic State.

            The six F/A-18 Hornets of the RAAF Air Task Group will operate as part of the coalition air contingent, hitting IS targets in the city.

            The RAAF KC-30A refueling aircraft will support the air campaign, as will the E-7A Wedgetail airborne warning and control aircraft.

            Closest to Australian boots on the ground could be the 80-stong special operations task group whose members have advised and mentored Iraq's elite Counter-Terrorism Service.

            This unit, referred to as the Golden Division, played a key role in the fight to retake Ramadi.

            Iraqi infantry trained by the 300 Australians and 100 New Zealanders of Task Group Taji will be in the thick of the fighting.

            Another 30 Australian personnel are embedded in coalition headquarters in Baghdad.

            US Lieutenant General Stephen Townsend, commander of the coalition taskforce, said the operation to regain control of Mosul would likely continue for weeks, possibly longer.

            He said Iraq was supported by a wide range of coalition capabilities, including air support, artillery, intelligence, advisors and forward air controllers.

            'But to be clear, the thousands of ground combat forces who will liberate Mosul are all Iraqis,' he said in a statement.

            'This may prove to be a long and tough battle, but the Iraqis have prepared for it and we will stand by them.'";

        var rake = new SentenceSimilarityUnity.Rake(minCharLength: 4, maxWordsLength: 12);

        var result = rake.Run(text);

        Assert.IsNotNull(result);

        var result2 = rake.Run(string.Join('|', result.Select(pair => pair.Key)));

        Assert.IsNotNull(result2);

        // 결과 값을 로그로 출력
        Debug.Log("전체 결과:");
        foreach (var pair in result)
        {
            Debug.Log($"키워드: {pair.Key}, 점수: {pair.Value}");
        }
    }
}
