# Nana �v���O���~���O����

## �͂��߂�

Nana �v���O���~���O����� .NET Framework �œ����v���O���~���O����ł��B
Nana �́u������E�ɑ���������I�v���]�ɊJ�����n�߂܂����B
���̂���͂܂� **R����** �̑��݂�m��܂���ł����B
�ق��ɕ��@�Ŗڎw���Ă���Ƃ����
* �Ȃ�ׂ���l�Ń����e�i���X�ł���悤�ɁA�R���p�C���̎������P���ł��邱��
* �Ȃ�ׂ��^�C�v���Ȃ��ł����悤�ɍςނ悤�ȕ��@�ł��邱��
�ł��B

�����x�͒Ⴍ�A�܂��ȒP�ȕ��@�����r���h�ł��܂���B
�G���[�����Ȃǂ��A�܂��낭�Ɏ����ł��Ă��܂���B
�Ƃ肠�������������悤�ɂȂ�܂����Ƃ��������ł��B

## ���s��

Nana ���g���ɂ�

Windows XP 32bit
Microsoft.NET Framework v2.0

���K�v�ł��B
�����Av2.0 �ȏ�� .NET �������Ă���Γ����Ǝv���܂��B

## ��������

�Ƃ肠���� c:\tmp �f�B���N�g��������Ƃ��āA
�����Ɏ���3�̃t�@�C�����_�E�����[�h���܂��B

https://github.com/quwahara/Nana/raw/master/Nana/bin/Release/Nana.exe
https://github.com/quwahara/Nana/raw/master/Nana/bin/Release/NanaLib.dll
https://github.com/quwahara/Nana/raw/master/Nana/bin/Release/HelloWorld.nana

cmd.exe ���J���Ac:tmp �̉��Ɉړ����A���̂悤�ɓ��́A���^�[�����܂��B

```
C:\Tmp>Nana.exe HelloWorld.nana
```

���܂������Ǝ��̂悤�ȃ��b�Z�[�W���o�܂��B

```
Microsoft (R) .NET Framework IL Assembler.  Version 2.0.50727.3053
Copyright (c) Microsoft Corporation.  All rights reserved.
Assembling 'HelloWorld.il'  to EXE --> 'HelloWorld.exe'
Source file is ANSI

Assembled global method .cctor
Assembled global method 0
Creating PE file

Emitting classes:

Emitting fields and methods:
Global  Methods: 2;

Emitting events and properties:
Global
Writing PE file
Operation completed successfully



C:\Tmp>

```

�����ē����f�B���N�g����  HelloWorld.exe ���ł��Ă��̂�
��������s����Ɖ��̂悤�ɏo�܂��B

```
C:\Tmp>HelloWorld.exe
Hello, world!

```
�����ł���A���߂łƂ��������܂��I
���Ȃ��̊��ŗ��h�ɓ����܂����B


���܂������Ȃ��Ƃ��́A���̂��Ƃ������Ă݂ĉ������B
Nana�ł� ilasm.exe �Ƃ����v���O���������s���Ă��܂��B
����͒ʏ�A���̃f�B���N�g���ɔz�u����Ă��Ă܂��B
Nana �ł͋K��ł����� ilasm.exe �����邱�Ƃ����҂��Ă��܂��B
```
C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727\ilasm.exe
```


���ϐ� NANA_ILASM_PATH �Ŏ��̂悤�ɒ��� ilasm.exe �̏ꏊ���w��ł��܂��B
���g���̊��� ilasm.exe ������Ƃ�����m�F���Ďw�肵�ĉ������B

```
c:\tmp> set NANA_ILASM_PATH=C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727\ilasm.exe
```
or
```
c:\tmp> set NANA_ILASM_PATH=C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\ilasm.exe
```


## ���C�Z���X

MIT ���C�Z���X�Ō��J���Ă��܂��B 
���C�Z���X�̑S���͉��ŎQ�Ƃł��܂��B

https://raw.github.com/quwahara/Nana/master/LICENSE

