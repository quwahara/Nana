# Nana �v���O���~���O����

## �͂��߂�

Nana �v���O���~���O����� .NET Framework �œ����v���O���~���O����ł��B
Nana �́u������E�ɑ���������I�v���]�ɊJ�����n�߂܂����B
���̂���͂܂� **R����** �̑��݂�m��܂���ł����B
�ق��ɖڎw���Ă���Ƃ����
* �Ȃ�ׂ���l�Ń����e�i���X�ł���悤�ɁA�R���p�C���̎������P���ł��邱��
* �Ȃ�ׂ��^�C�v���Ȃ��ł����悤�ɍςނ悤�ȕ��@�ł��邱��

�ł��B

�����x�͒Ⴍ�A�܂��ȒP�ȕ��@�����r���h�ł��܂���B
�G���[�����Ȃǂ��A�܂��낭�Ɏ����ł��Ă��܂���B
�Ƃ肠�������������悤�ɂȂ�܂����Ƃ��������ł��B

## ���s��

Nana ���g���ɂ�
* Windows XP 32bit
* Microsoft.NET Framework v2.0

���K�v�ł��B
���� v2.0 �ȏ�� .NET �������Ă���΁AWidows Vsita �� 7 �ł������Ǝv���܂��B

## ��������

�Ƃ肠������� c:\tmp �f�B���N�g��������Ƃ��āA
�����Ɏ���3�̃t�@�C�����_�E�����[�h���܂��B

https://github.com/quwahara/Nana/raw/master/Nana/bin/Release/Nana.exe
https://github.com/quwahara/Nana/raw/master/Nana/bin/Release/NanaLib.dll
https://github.com/quwahara/Nana/raw/master/Nana/bin/Release/HelloWorld.nana

cmd.exe ���J���Ac:\tmp �̉��Ɉړ����A���̂悤�ɓ��́A���^�[�����܂��B

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

�����ē����f�B���N�g����  HelloWorld.exe ���ł��Ă��܂��B
��������s����Ɖ��̂悤�ɏo�܂��B

```
C:\Tmp>HelloWorld.exe
Hello, world!

```
���߂łƂ��������܂��I
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

### Mac �ł� mono ���C���X�g�[�����Ď��̂悤�ɂ��Ď����Ă݂ĉ����� 
```
$ export NANA_ILASM_PATH=/usr/bin/ilasm
$ mono Nana.exe HelloWorld.nana
$ mono HelloWorld..exe
```


## ����
0 ���� 16 �܂ł̃t�B�{�i�b�`�����\�����܂�
```
//  Save as 'fibo.nana'
//  build:
//  > nana fibo
fun Fibo(n:int):int
..
    if      0 == n then
        return 0
    elif    1 == n then
        return 1
    else
        return Fibo(n - 2) + Fibo(n - 1)
    end
,,

num = 0
while   17 > num do
    Fibo(num)   -> fi
    `p(fi)
    num = num + 1
end
```

## ���@

### �������e����

```
i1  = 1                 //  int�^��1�ɂȂ�܂��B
l1  = 1L                //  long�^��1�ɂȂ�܂��B
u1  = 1U                //  uint�^��1�ɂȂ�܂��B
ul1 = 1UL               //  ulong�^��1�ɂȂ�܂��B

//  #16�i���ɂ�鐮�����e����(�悭'0x'�Ŏn�܂�)�L�@�ɂ͂܂�*�Ή����Ă��܂���B*
```

### �������e����

```
d1  = 0.1                       //  �������w�肷��� double�^�ɂȂ�܂�
f1  = 0.1F                      //  suffix 'F' ���w�肷��� float�^�ɂȂ�܂�
d2  = 314E-2                    //  'E' 10�̊K��̎w�����w��ł��܂��Bdouble�^�ɂȂ�܂��B
```

### ���l�^�L���X�g

```
//  "as" �̌�Ɏw�肳��Ă���^�ɃL���X�g���܂��B
// �����ӂꂵ���Ƃ��͐؂�̂Ă��܂��B

d1   = 1 as double      //  d1 �� double�^��1�ɂȂ�܂��B
b1   = 1 as byte        //  b1 �� byte�^��1�ɂȂ�܂��B

//  "as!" �̌�Ɏw�肳��Ă���^�ɃL���X�g���܂��B
// �����ӂꂵ���Ƃ��� System.OverflowException ����������܂��B

b2   = 200 as! byte     //  b2 �� byte�^��200�ɂȂ�܂��B
b3   = 300 as! byte     //  System.OverflowException �����������
```

### �Q�ƌ^�L���X�g

```
//  "as" �̌�Ɏw�肳��Ă���^�ɃL���X�g���܂��B
//  �L���X�g�Ɏ��s�����Ƃ��� null �ɂȂ�܂��B

o   = "hi" as object    // => o �� string�^ "hi" ������ object �^�̕ϐ��ɂȂ�

p   = 1 as object       // => p �� null ������ object �^�̕ϐ��ɂȂ�


//  "as!" �̌�Ɏw�肳��Ă���^�ɃL���X�g���܂��B
//  �L���X�g�Ɏ��s�����Ƃ��� System.InvalidCastException ����������܂��B

q   = "hi" as! object   // => q �� string�^ "hi" ������ object �^�̕ϐ��ɂȂ�

r   = 1 as! string      // => System.InvalidCastException �����������
```

## TODO

�����ȉ��Z�q���w�肵���Ƃ��̃��b�Z�[�W�𕪂���₷��
���l�^�ϊ�
����widening�ƃ��\�b�h�Ăяo���̉���


## ���C�Z���X

MIT ���C�Z���X�Ō��J���Ă��܂��B 
���C�Z���X�̑S���͉��ŎQ�Ƃł��܂��B

https://raw.github.com/quwahara/Nana/master/LICENSE

