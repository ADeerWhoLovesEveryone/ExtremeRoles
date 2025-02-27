# Extreme Roles(ExR) C# Style Guide

# �T�v

�����ExtremeRoles(ExR)��C#�R�[�h�J���ɂ�����X�^�C���K�C�h���C���ł��B
��{�I��Microsoft��.NET�̃X�^�C��/�R�[�f�B���O�K�C�h���C���ɉ����ĊJ������Ă��܂����AUnity��Mod�J���̂��߂������̓_�ŕύX���������Ă��܂�

# ����
* **�ǐ�:** �R�[�h�͊Ȍ����킩��₷�����ׂ�
* **�_�:** �R�[�h�͕ύX���e�Ղ��A�_���������Ԃ�ۂׂ�
* **�p�t�H�[�}���X:** �R�[�h�͉ǐ��Ə_��͈ێ����A�ł������̍��������������ׂ��ł���

# �ύX�ӏ�

## �A�N�Z�T�r���e�B
* **private�t�@�[�X�g:** �ϐ��̃X�R�[�v�͂Ȃ�ׂ������A�C���X�^���X�ϐ����͌��J����K�v���Ȃ��ꍇ��private�ɂ���
* **readonly/�v���p�e�B�𑽗l:** �s�K�v�ȏ��������������������Ȃ��悤�ɂ���
    * �N���X�ϐ��͂Ȃ�ׂ�readonly�ϐ���Get�v���p�e�B�ɂ��� `private readonly` `public Git git { get; }` 
    * �R���N�V�����̎󂯓n����ϐ��͉\�Ȍ���readonly�̃C���^�[�t�F�[�X���g�p���� `IReadOnlyList<Net>` `IReadOnlyDictionary<string, string>`

## �N���X
* **�p�������Ϗ�** ��{�I�ɃN���X��sealed�����ăV�[�����A�p�����l����O�ɈϏ��ł��Ȃ������l����
* **�s�K�v��new** �s�K�v��new��h�����߁A�C���X�^���X�ϐ��������N���Xstaic�����Ȃ�ׂ�static�N���X������

## �^
* **var:** �g�ݍ��݌^�͌^���g�p���Avar�͌^���m���ɂ킩�鎞�Ɏg�p����

## Null
* **Null���e�^(Nullable):** Null�̉\���̂���R�[�h��Nullable���g�p���ANull�`�F�b�N��K���s��
    
    ```csharp
    #nullable enable

    public sealed class MyClassA
    {
        public void MyMethod(MyClassB b) // b��Null�ł͂Ȃ����Ƃ�ۏ� 
        {
        }
    }


    public sealed class MyClassB
    {
        public void MyMethod(MyClassA? a) // a��Null�̉\��������
        {
            if (a is not null) // null�`�F�b�N�͕K�������Ȃ�
            {
                // ����
            }
        }

        // �K�v�ɉ�����NotNullWhen�����g��
        public bool NullCheck([NotNullWhen(true)] out MyClassA? a) // ���̃��\�b�h��True��Ԃ����Aa��Null�ł͂Ȃ�
        {
        }
    }

    ```


## Unity
* **�p�t�H�[�}���X:** Unity�̃Q�[����MOD���邪�̂Ƀp�t�H�[�}���X�͏�ɒǂ����߂�ׂ��ł���
* **�ǐ��ƃp�t�H�[�}���X�̃g���[�h�I�t:** �ǐ��ƃp�t�H�[�}���X���g���[�h�I�t�̏ꍇ�A�ǐ������߂邱�Ƃ��d�v������
* **Unity�Ǝ���Null�`�F�b�N:** Unity�̃N���X��Null�`�F�b�N���I�[�o���C�h����Ǝ���������Ă���
    * Unity�̃N���X�������͌p�����ꂽ�N���X��Null�`�F�b�N�͎Z�p���Z�q��p���ă`�F�b�N���s��
        * Null�������Z�q��is�ɂ���r�͐�΂ɍs��Ȃ�
    * Unity�̃N���X�������͌p�����ꂽ�N���X�̕ϐ��݂͎̂̂g�p���Ȃ�
    ```csharp
    public sealed class MyMono : MonoBehavior
    {
    }

    public sealed class MyClassC
    {
        public void MyMethod(MyMono? mono)
        {
            if (mono != null)
            {
                // ����
            }
        }
    }
    ```

## �����K��
* **�ϐ�/�萔**
    * **public/protected:** �@�p�X�J���P�[�X: `RoleManager`, `BoolOption`
    * **private:** �@�L�������P�[�X: `gameResult`, `userData`
* **���\�b�h/�֐�**
    * **public/protected:** �@�p�X�J���P�[�X: `CalculateTotal()`, `ProcessData()`
    * **private:** �@�L�������P�[�X: `computeResult()`, `resolveData()`
* **�N���X/���R�[�h/�\����:** �@�p�X�J���P�[�X: `UserManager`, `PaymentProcessor`
* **�C���^�[�t�F�[�X:** I����n�܂�p�X�J���P�[�X: `IRole`, `IMeetingHud`

## ���O���
* **using�f�B���N�e�B�u:** �ȉ��̏��ԂŐ錾���A���̒��ŃA���t�@�x�b�g���Ƀ\�[�g���� 
    * .NET�W��
    * �O�����C�u����/DLL
    * ���g�̃��C�u����/DLL
* **�G�C���A�X:** �K�v�ɉ����ăG�C���A�X�����Ausing�f�B���N�e�B�u�Ɩ��O��Ԃ̐錾�̊Ԃɒǉ�����
* **���O��Ԃ̐錾:** �t�@�C���X�R�[�v���O��Ԃ��g�p����

    ```csharp
    using System.Collection.Generic;
    using System.Ling;

    using InnerNet;

    using MyMod.Module;

    using Heep = MyMod.Module.Heep;

    namespace MyMod.Collection;
    
    // �R�[�h

    ```

# ��

```csharp
using System.Collection.Generic;

using SemanticVersioning;

using SupportedLangs = MyLib.Translation.SupportedLangs;

namespace MyLib.Beta;

#nullable enable

public sealed class BetaContentAdder(string version)
{
    public const string NewTransDataPath = "ExtremeRoles.Beta.Resources.JsonData.TextRevamp.json";

	private readonly Version version = new Version(version);

	private const string transKey = "PublicBetaContent";

	public void AddContentText(
		SupportedLangs curLang,
		Dictionary<string, string> transData)
	{
		string content = curLang switch
		{
			SupportedLangs.Japanese =>
				"�E��E�����̃e�L�X�g�̉��P/�ύX\n�E�t�B�[�h�o�b�N�V�X�e���̒ǉ�\n�E�u���肷��v�{�^�����g�O�����ɕύX",
			_ => "",
		};
		transData.Add(transKey, content);

		// �t�B�[�h�o�b�N�𑗂�悤
		transData.Add("SendFeedBackToExR", curLang switch
		{
			SupportedLangs.Japanese => "�t�B�[�h�o�b�N�𑗂�",
			_ => "",
		});
	}
}

```